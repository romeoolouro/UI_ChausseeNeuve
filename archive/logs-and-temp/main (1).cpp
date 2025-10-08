#include <iostream>
#include <vector>
#include <boost/math/special_functions/bessel.hpp>
#include <cmath>
#include <algorithm>
using namespace std;
using namespace boost::math;
double nbrecouche = 2, roue, io, ki;
double Poids, a, d;
vector<double> Mu(nbrecouche), Young(nbrecouche), epais(nbrecouche), tabInterface(nbrecouche - 1);
void mat_calcul(vector<double>& mat, int option, vector<double>& matx_calcul)
{
    int i;
    int taille_mat_calcul = 2 * mat.size() - 1;// taille de la matrice à renvoyer
    vector<double> mat_aux((taille_mat_calcul + 1), 0);//Initialisation de la matrice intermédiaire de taille 2n avec des zéros
    // Remplissage de la mat auxiliaire
    for (i = 0; i < (taille_mat_calcul + 1); i++)
    {
        mat_aux[i] = mat[(i / 2)];
    }
    //Transformation de mat = [ a1, a2, ..., an-1, an] en matx_calcul = [0, a1, a1, a2, a2, ..., an-1, an-1, an, an]
    int const taille = taille_mat_calcul + 2;
    //vector<double> matx_calcul (taille) ;
    if (option == 1)
    {
        matx_calcul[0] = 0;
        for (i = 0; i < (taille_mat_calcul + 1); i++)
        {
            matx_calcul[i + 1] = mat_aux[i];
        }
    }
    //Transformation de mat = [ a1, a2, ..., an-1, an] en matx_calcul = [a1, a1, a2, a2, ..., an-1, an-1, an, an]
    if (option == 2)
    {
        for (i = 0; i < (taille_mat_calcul + 1); i++)
        {
            matx_calcul[i] = mat_aux[i];
        }
    }
}

void cal_altitude(vector<double>& mat_epais)
{
    int i;
    int taille = mat_epais.size();
    for (i = 1; i < taille; i++)
    {
        mat_epais[i] += mat_epais[i - 1];
    }
}

void gaussLegendre(int n, vector<double>& points, vector<double>& weights)
{
    double EPS = 1e-15;  // Tolérance pour la précision des calculs

    // Calcul des points et des poids
    for (int i = 0; i < n; i++) {
        // Initialisation des approximations initiales pour les points et les poids
        double x = std::cos(3.14159265358979 * (i + 0.75) / (n + 0.5));
        double w = 0.0;

        double error;
        do {
            double p1 = 1.0;
            double p2 = 0.0;
            for (int j = 0; j < n; j++) {
                double p3 = p2;
                p2 = p1;
                p1 = ((2.0 * j + 1.0) * x * p2 - j * p3) / (j + 1.0);
            }

            // Calcul de la dérivée de Pn(x)
            double dp = n * (x * p1 - p2) / (x * x - 1.0);

            // Mise à jour de l'approximation du point et du poids
            double dx = p1 / dp;
            x -= dx;
            w = 2.0 / ((1.0 - x * x) * dp * dp);

            error = std::abs(dx);
        } while (error > EPS);

        // Stockage des points et des poids calculés
        points[i] = x;
        weights[i] = w;
    }
    double c = 0;
    for (int i = 0; i < (n / 2); i++)
    {
        c = points[i];
        points[i] = points[n - 1 - i];
        points[n - 1 - i] = c;
    }
}
void  GaussianQuadratureWeights(int n, double borne_inf, double borne_sup, vector<vector<double>>& points_poids)
{
    vector<double> points(n);
    vector<double> poids(n);
    gaussLegendre(n, points, poids);
    for (int i = 0; i < n; i++)
    {
        points[i] = (borne_sup - borne_inf) / 2 * points[i] + (borne_sup + borne_inf) / 2;
        poids[i] *= (borne_sup - borne_inf) / 2;
    }
    for (int i = 0; i < n; i++)
    {
        points_poids[i][0] = points[i];
        points_poids[i][1] = poids[i];
    }
}


vector<double> MuCalcul(2 * nbrecouche);

vector<double> zcalcul(2 * nbrecouche + 1);

vector<double> YoungCalcul(2 * nbrecouche);

int k = 4 * nbrecouche - 2;
void det_MFINI(double m, int nbrecouche, vector<double>& Mu, vector<double>& Young, vector<double>& epais, vector<double>& tabInterface, vector<vector<double>>& MFINI)
{
    //Programme permettant de remplir une matrice avec d'autres sur la diagonale d'une grande matrice MFINI
    int k = 4 * nbrecouche - 2;
    for (int a = 0; a < k; a++)
    {
        for (int b = 0; b < k; b++)
        {
            MFINI[a][b] = 0;
        }
    }
    for (int ni = 1; ni < nbrecouche + 1; ni++) // ni pour couche i
    {
        int dl, dc1, dc2, limM1, mult_moins1;
        int i = ni - 1;
        double M1[4][4] = { 0 };
        double M2[4][4] = { 0 };

        if (ni == 1) // Couche 1
        {
            double M01[2][2] = { m * m,m * (1 - 2 * Mu[0]),-m * m,2 * m * Mu[0] };
            double M02[2][2] = { m * m,-m * (1 - 2 * Mu[0]),m * m,2 * m * Mu[0] };
            for (int a = 0; a < 2; a++)
            {
                for (int b = 0; b < 2; b++)
                {
                    M1[a][b] = M01[a][b];
                    M2[a][b] = M02[a][b];
                }
            }

            dl = 2;         // dl : Pour étendre le nombre de ligne au début
            dc1 = 4;          // dc1 : Pour étendre le nombre de colonne au début
            dc2 = 0;        // dc2 : Pour étendre le nombre de colonne à la fin
            limM1 = 2;         // limM1 : Pour dimunier la limite de M1
            mult_moins1 = 1;  // mult_moins1 : Facteur permettant de multiplier les Mi2 par -1 avant de les mettre dans la matrice MFINI (sauf pour les matrices MAB0 et MCD0)
        }
        else if (ni == nbrecouche)
        {
            double Mi1[4][4];
            double Mi2[4][4];
            if (tabInterface[i - 1] == 0) //Cas d'interface collée
            {
                Mi1[0][0] = m * m * exp(-m * epais[i - 1]);
                Mi1[0][1] = m * (1 - 2 * Mu[i - 1] + m * epais[i - 1]) * exp(-m * epais[i - 1]);
                Mi1[0][2] = m * m * exp(m * epais[i - 1]);
                Mi1[0][3] = -m * (1 - 2 * Mu[i - 1] - m * epais[i - 1]) * exp(m * epais[i - 1]);
                Mi1[1][0] = (m * m) * (1 + Mu[i - 1]) / Young[i - 1] * exp(-m * epais[i - 1]);
                Mi1[1][1] = m * (2 - 4 * Mu[i - 1] + m * epais[i - 1]) * ((1 + Mu[i - 1]) / Young[i - 1]) * exp(-m * epais[i - 1]);
                Mi1[1][2] = -m * m * (1 + Mu[i - 1]) / Young[i - 1] * exp(m * epais[i - 1]);
                Mi1[1][3] = m * (2 - 4 * Mu[i - 1] - m * epais[i - 1]) * ((1 + Mu[i - 1]) / Young[i - 1]) * exp(m * epais[i - 1]);
                Mi1[2][0] = -(m * m) * exp(-m * epais[i - 1]);
                Mi1[2][1] = m * (2 * Mu[i - 1] - m * epais[i - 1]) * exp(-m * epais[i - 1]);
                Mi1[2][2] = m * m * exp(m * epais[i - 1]);
                Mi1[2][3] = m * (2 * Mu[i - 1] + m * epais[i - 1]) * exp(m * epais[i - 1]);
                Mi1[3][0] = m * m * (1 + Mu[i - 1]) / Young[i - 1] * exp(-m * epais[i - 1]);
                Mi1[3][1] = -m * (1 - m * epais[i - 1]) * (1 + Mu[i - 1]) / Young[i - 1] * exp(-m * epais[i - 1]);
                Mi1[3][2] = (m * m) * (1 + Mu[i - 1]) / Young[i - 1] * exp(m * epais[i - 1]);
                Mi1[3][3] = m * (1 + m * epais[i - 1]) * (1 + Mu[i - 1]) / Young[i - 1] * exp(m * epais[i - 1]);

                Mi2[0][0] = m * m * exp(-m * epais[i - 1]);
                Mi2[0][1] = m * (1 - 2 * Mu[i] + m * epais[i - 1]) * exp(-m * epais[i - 1]);
                Mi2[0][2] = 0;
                Mi2[0][3] = 0;
                Mi2[1][0] = (m * m) * (1 + Mu[i]) / Young[i] * exp(-m * epais[i - 1]);
                Mi2[1][1] = (m * (2 - 4 * Mu[i] + m * epais[i - 1]) * (1 + Mu[i]) / Young[i]) * exp(-m * epais[i - 1]);
                Mi2[1][2] = 0;
                Mi2[1][3] = 0;
                Mi2[2][0] = -m * m * exp(-m * epais[i - 1]);
                Mi2[2][1] = m * (2 * Mu[i] - m * epais[i - 1]) * exp(-m * epais[i - 1]);
                Mi2[2][2] = 0;
                Mi2[2][3] = 0;
                Mi2[3][0] = m * m * (1 + Mu[i]) / Young[i] * exp(-m * epais[i - 1]);
                Mi2[3][1] = -m * (1 - m * epais[i - 1]) * (1 + Mu[i]) / Young[i] * exp(-m * epais[i - 1]);
                Mi2[3][2] = 0;
                Mi2[3][3] = 0;
            }
            if (tabInterface[i - 1] == 2) //Cas d'interface décollée
            {
                Mi1[0][0] = (m * m) * exp(-m * epais[i - 1]);
                Mi1[0][1] = m * (1 - 2 * Mu[i - 1] + m * epais[i - 1]) * exp(-m * epais[i - 1]);
                Mi1[0][2] = m * m * exp(m * epais[i - 1]);
                Mi1[0][3] = -m * (1 - 2 * Mu[i - 1] - m * epais[i - 1]) * exp(m * epais[i - 1]);
                Mi1[1][0] = (m * m) * (1 + Mu[i - 1]) / Young[i - 1] * exp(-m * epais[i - 1]);
                Mi1[1][1] = (m * (2 - 4 * Mu[i - 1] + m * epais[i - 1]) * (1 + Mu[i - 1]) / Young[i - 1]) * exp(-m * epais[i - 1]);
                Mi1[1][2] = -m * m * (1 + Mu[i - 1]) / Young[i - 1] * exp(m * epais[i - 1]);
                Mi1[1][3] = m * (2 - 4 * Mu[i - 1] - m * epais[i - 1]) * ((1 + Mu[i - 1]) / Young[i - 1]) * exp(m * epais[i - 1]);
                Mi1[2][0] = -(m * m) * exp(-m * epais[i - 1]);
                Mi1[2][1] = m * (2 * Mu[i - 1] - m * epais[i - 1]) * exp(-m * epais[i - 1]);
                Mi1[2][2] = m * m * exp(m * epais[i - 1]);
                Mi1[2][3] = m * (2 * Mu[i - 1] + m * epais[i - 1]) * exp(m * epais[i - 1]);
                Mi1[3][0] = 0;
                Mi1[3][1] = 0;
                Mi1[3][2] = 0;
                Mi1[3][3] = 0;


                Mi2[0][0] = m * m * exp(-m * epais[i - 1]);
                Mi2[0][1] = m * (1 - 2 * Mu[i] + m * epais[i - 1]) * exp(-m * epais[i - 1]);
                Mi2[0][2] = 0;
                Mi2[0][3] = 0;
                Mi2[1][0] = (m * m) * (1 + Mu[i]) / Young[i] * exp(-m * epais[i - 1]);
                Mi2[1][1] = (m * (2 - 4 * Mu[i] + m * epais[i - 1]) * (1 + Mu[i]) / Young[i]) * exp(-m * epais[i - 1]);
                Mi2[1][2] = 0;
                Mi2[1][3] = 0;
                Mi2[2][0] = 0;
                Mi2[2][1] = 0;
                Mi2[2][2] = 0;
                Mi2[2][3] = 0;
                Mi2[3][0] = -m * m * exp(-m * epais[i - 1]);
                Mi2[3][1] = m * (2 * Mu[i] - m * epais[i - 1]) * exp(-m * epais[i - 1]);
                Mi2[3][2] = 0;
                Mi2[3][3] = 0;
            }

            for (int a = 0; a < 4; a++)
            {
                for (int b = 0; b < 4; b++)
                {
                    M1[a][b] = Mi1[a][b];
                    M2[a][b] = Mi2[a][b];
                }
            }

            dl = 0;
            dc1 = 0;
            dc2 = 2;
            limM1 = 0;
            mult_moins1 = -1;
        }
        else  // Couche(s) intermédiaire(s)
        {
            double Mi1[4][4];
            double Mi2[4][4];
            if (tabInterface[i - 1] == 0)    //Cas d'interface collée
            {
                Mi1[0][0] = m * m * exp(-m * epais[i - 1]);
                Mi1[0][1] = m * (1 - 2 * Mu[i - 1] + m * epais[i - 1]) * exp(-m * epais[i - 1]);
                Mi1[0][2] = m * m * exp(m * epais[i - 1]);
                Mi1[0][3] = -m * (1 - 2 * Mu[i - 1] - m * epais[i - 1]) * exp(m * epais[i - 1]);
                Mi1[1][0] = (m * m) * (1 + Mu[i - 1]) / Young[i - 1] * exp(-m * epais[i - 1]);
                Mi1[1][1] = (m * (2 - 4 * Mu[i - 1] + m * epais[i - 1]) * (1 + Mu[i - 1]) / Young[i - 1]) * exp(-m * epais[i - 1]);
                Mi1[1][2] = -m * m * (1 + Mu[i - 1]) / Young[i - 1] * exp(m * epais[i - 1]);
                Mi1[1][3] = m * (2 - 4 * Mu[i - 1] - m * epais[i - 1]) * ((1 + Mu[i - 1]) / Young[i - 1]) * exp(m * epais[i - 1]);
                Mi1[2][0] = -(m * m) * exp(-m * epais[i - 1]);
                Mi1[2][1] = m * (2 * Mu[i - 1] - m * epais[i - 1]) * exp(-m * epais[i - 1]);
                Mi1[2][2] = m * m * exp(m * epais[i - 1]);
                Mi1[2][3] = m * (2 * Mu[i - 1] + m * epais[i - 1]) * exp(m * epais[i - 1]);
                Mi1[3][0] = m * m * (1 + Mu[i - 1]) / Young[i - 1] * exp(-m * epais[i - 1]);
                Mi1[3][1] = -m * (1 - m * epais[i - 1]) * (1 + Mu[i - 1]) / Young[i - 1] * exp(-m * epais[i - 1]);
                Mi1[3][2] = (m * m) * (1 + Mu[i - 1]) / Young[i - 1] * exp(m * epais[i - 1]);
                Mi1[3][3] = m * (1 + m * epais[i - 1]) * (1 + Mu[i - 1]) / Young[i - 1] * exp(m * epais[i - 1]);

                Mi2[0][0] = m * m * exp(-m * epais[i - 1]);
                Mi2[0][1] = m * (1 - 2 * Mu[i] + m * epais[i - 1]) * exp(-m * epais[i - 1]);
                Mi2[0][2] = m * m * exp(m * epais[i - 1]);
                Mi2[0][3] = -m * (1 - 2 * Mu[i] - m * epais[i - 1]) * exp(m * epais[i - 1]);
                Mi2[1][0] = (m * m) * (1 + Mu[i]) / Young[i] * exp(-m * epais[i - 1]);
                Mi2[1][1] = (m * (2 - 4 * Mu[i] + m * epais[i - 1]) * (1 + Mu[i]) / Young[i]) * exp(-m * epais[i - 1]);
                Mi2[1][2] = -m * m * (1 + Mu[i]) / Young[i] * exp(m * epais[i - 1]);
                Mi2[1][3] = (m * (2 - 4 * Mu[i] - m * epais[i - 1]) * (1 + Mu[i]) / Young[i]) * exp(m * epais[i - 1]);
                Mi2[2][0] = -m * m * exp(-m * epais[i - 1]);
                Mi2[2][1] = m * (2 * Mu[i] - m * epais[i - 1]) * exp(-m * epais[i - 1]);
                Mi2[2][2] = m * m * exp(m * epais[i - 1]);
                Mi2[2][3] = m * (2 * Mu[i] + m * epais[i - 1]) * exp(m * epais[i - 1]);
                Mi2[3][0] = m * m * (1 + Mu[i]) / Young[i] * exp(-m * epais[i - 1]);
                Mi2[3][1] = -m * (1 - m * epais[i - 1]) * (1 + Mu[i]) / Young[i] * exp(-m * epais[i - 1]);
                Mi2[3][2] = (m * m) * (1 + Mu[i]) / Young[i] * exp(m * epais[i - 1]);
                Mi2[3][3] = m * (1 + m * epais[i - 1]) * (1 + Mu[i]) / Young[i] * exp(m * epais[i - 1]);
            }
            if (tabInterface[i - 1] == 2) //Cas d'interface décollée
            {
                Mi1[0][0] = (m * m) * exp(-m * epais[i - 1]);
                Mi1[0][1] = m * (1 - 2 * Mu[i - 1] + m * epais[i - 1]) * exp(-m * epais[i - 1]);
                Mi1[0][2] = m * m * exp(m * epais[i - 1]);
                Mi1[0][3] = -m * (1 - 2 * Mu[i - 1] - m * epais[i - 1]) * exp(m * epais[i - 1]);
                Mi1[1][0] = (m * m) * (1 + Mu[i - 1]) / Young[i - 1] * exp(-m * epais[i - 1]);
                Mi1[1][1] = (m * (2 - 4 * Mu[i - 1] + m * epais[i - 1]) * (1 + Mu[i - 1]) / Young[i - 1]) * exp(-m * epais[i - 1]);
                Mi1[1][2] = -m * m * (1 + Mu[i - 1]) / Young[i - 1] * exp(m * epais[i - 1]);
                Mi1[1][3] = m * (2 - 4 * Mu[i - 1] - m * epais[i - 1]) * ((1 + Mu[i - 1]) / Young[i - 1]) * exp(m * epais[i - 1]);
                Mi1[2][0] = -(m * m) * exp(-m * epais[i - 1]);
                Mi1[2][1] = m * (2 * Mu[i - 1] - m * epais[i - 1]) * exp(-m * epais[i - 1]);
                Mi1[2][2] = m * m * exp(m * epais[i - 1]);
                Mi1[2][3] = m * (2 * Mu[i - 1] + m * epais[i - 1]) * exp(m * epais[i - 1]);
                Mi1[3][0] = 0;
                Mi1[3][1] = 0;
                Mi1[3][2] = 0;
                Mi1[3][3] = 0;

                Mi2[0][0] = m * m * exp(-m * epais[i - 1]);
                Mi2[0][1] = m * (1 - 2 * Mu[i] + m * epais[i - 1]) * exp(-m * epais[i - 1]);
                Mi2[0][2] = m * m * exp(m * epais[i - 1]);
                Mi2[0][3] = -m * (1 - 2 * Mu[i] - m * epais[i - 1]) * exp(m * epais[i - 1]);
                Mi2[1][0] = (m * m) * (1 + Mu[i]) / Young[i] * exp(-m * epais[i - 1]);
                Mi2[1][1] = (m * (2 - 4 * Mu[i] + m * epais[i - 1]) * (1 + Mu[i]) / Young[i]) * exp(-m * epais[i - 1]);
                Mi2[1][2] = -(m * m) * (1 + Mu[i]) / Young[i] * exp(m * epais[i - 1]);
                Mi2[1][3] = (m * (2 - 4 * Mu[i] - m * epais[i - 1]) * (1 + Mu[i]) / Young[i]) * exp(m * epais[i - 1]);
                Mi2[2][0] = 0;
                Mi2[2][1] = 0;
                Mi2[2][2] = 0;
                Mi2[2][3] = 0;
                Mi2[3][0] = -m * m * exp(-m * epais[i - 1]);
                Mi2[3][1] = m * (2 * Mu[i] - m * epais[i - 1]) * exp(-m * epais[i - 1]);
                Mi2[3][2] = m * m * exp(m * epais[i - 1]);
                Mi2[3][3] = m * (2 * Mu[i] + m * epais[i - 1]) * exp(m * epais[i - 1]);
            }
            for (int a = 0; a < 4; a++)
            {
                for (int b = 0; b < 4; b++)
                {
                    M1[a][b] = Mi1[a][b];
                    M2[a][b] = Mi2[a][b];
                }
            }
            dl = 0;
            dc1 = 0;
            dc2 = 0;
            limM1 = 0;
            mult_moins1 = -1;
        }
        int rows = 0;
        for (int j = 4 * ni + dl - 6; j < 4 * ni - 2; j++)
        {
            int col = 0;
            for (int k = 4 * ni + dc1 - 8; k < 4 * ni + limM1 - 4; k++)
            {
                //Affectation à la sous matrice MFINI(i1 à im, j1 à jk)=M1
                MFINI[j][k] = M1[rows][col];
                col++;
            }
            col = 0;
            for (int k = 4 * ni + limM1 - 4; k < 4 * ni - dc2; k++)
            {
                //Affectation à la sous matrice MFINI(i1 à im, jk+1 à jn)=M2
                MFINI[j][k] = mult_moins1 * M2[rows][col];
                col++;
            }
            rows++;
        }
    }
}

void det_ABCD(double m, vector<double>& ABCD)
{
    //Détermination de An et Bn (ABn) puis de Ai Bi Ci et Di
       // Conditions à la surface
    int k = 4 * nbrecouche - 2;
    vector<double> M10(k, 0);
    M10[0] = 1;// Quelque soit le type de roue et son positionnement
    // Fonction pour inverser la matrice

      // Créer une matrice identité
    vector<vector<double>> matriceIdentite(k, vector<double>(k, 0));
    for (int i = 0; i < k; i++)
    {
        matriceIdentite[i][i] = 1.0;
    }

    // Copier la matrice d'origine dans une matrice temporaire
    vector<vector<double>> matriceTemp(k, vector<double>(k, 0));
    det_MFINI(m, nbrecouche, Mu, Young, epais, tabInterface, matriceTemp);

    // Appliquer l'algorithme de Gauss-Jordan pour inverser la matrice
    for (int i = 0; i < k; ++i)
    {
        // Trouver le pivot non nul
        if (matriceTemp[i][i] == 0.0)
        {
            int pivotLigne = -1;
            for (int j = i + 1; j < k; ++j)
            {
                if (matriceTemp[j][i] != 0.0)
                {
                    pivotLigne = j;
                    break;
                }
            }

            // Échanger les lignes
            if (pivotLigne == -1)
            {
                std::cout << "La matrice n'est pas inversible." << std::endl;
            }
            else
            {
                for (int h = 0; h < k; k++)
                {
                    double swap = matriceTemp[i][h];
                    matriceTemp[i][h] = matriceTemp[pivotLigne][h];
                    matriceTemp[pivotLigne][h] = swap;
                    swap = matriceIdentite[i][h];
                    matriceIdentite[i][h] = matriceIdentite[pivotLigne][h];
                    matriceIdentite[pivotLigne][h] = swap;
                }
            }
        }

        // Réduire la ligne pivot à un
        double pivot = matriceTemp[i][i];
        for (int j = 0; j < k; ++j)
        {
            matriceTemp[i][j] /= pivot;
            matriceIdentite[i][j] /= pivot;
        }

        // Éliminer les autres éléments de la colonne pivot
        for (int j = 0; j < k; ++j)
        {
            if (j != i)
            {
                double coefficient = matriceTemp[j][i];
                for (int h = 0; h < k; ++h)
                {
                    matriceTemp[j][h] -= coefficient * matriceTemp[i][h];
                    matriceIdentite[j][h] -= coefficient * matriceIdentite[i][h];
                }
            }
        }
    }

    // Calcul des coefficients Ai Bi Ci et Di
    //double ABCD [k] ;
    for (int i = 0; i < k; i++)
    {
        double mult = 0;
        for (int j = 0; j < k; j++)
        {
            mult += matriceIdentite[i][j] * M10[j];
        }
        ABCD[i] = mult;
    }
}

vector<double>ABCD(k);
double  Ai, Bi, Ci, Di;
double fsigmaz(double m1)
{
    //double  Ai, Bi, Ci, Di;
    //vector<double>ABCD(k);
    //det_ABCD(m1, ABCD);

    /*if (io != nbrecouche)
    {
        Ai = ABCD[4 * (io - 1)];
        Bi = ABCD[4 * (io - 1) + 1];
        Ci = ABCD[4 * (io - 1) + 2];
        Di = ABCD[4 * (io - 1) + 3];
    }
    else
    {
        Ai = ABCD[4 * (io - 1)];
        Bi = ABCD[4 * (io - 1) + 1];
        Ci = 0;
        Di = 0;
    }*/
    double f = Ai * (m1 * m1) * exp(-m1 * zcalcul[ki - 1]) + m1 * (1 - 2 * MuCalcul[ki - 1] + m1 * zcalcul[ki - 1]) * Bi * exp(-m1 * zcalcul[ki - 1]) + (m1 * m1) * Ci * exp(m1 * zcalcul[ki - 1]) - m1 * (1 - 2 * MuCalcul[ki - 1] - m1 * zcalcul[ki - 1]) * Di * exp(m1 * zcalcul[ki - 1]);
    return (f);
}

double fsigmar01(double m2)
{
    //double  Ai, Bi, Ci, Di;
    //vector<double>ABCD(k);
    //det_ABCD(m2, ABCD);
    Ci = ABCD[4 * (io - 1) + 2];
    Di = ABCD[4 * (io - 1) + 3];
    double f = 4 * (m2 * m2) * Ci + 8 * m2 * MuCalcul[ki - 1] * Di - 1;
    return (f);
}

double fsigmar1(double m3)
{
    //double  Ai, Bi, Ci, Di;
    //vector<double>ABCD(k);
    //det_ABCD(m3, ABCD);
    /*if (io != nbrecouche)
    {
        Ai = ABCD[4 * (io - 1)];
        Bi = ABCD[4 * (io - 1) + 1];
        Ci = ABCD[4 * (io - 1) + 2];
        Di = ABCD[4 * (io - 1) + 3];
    }
    else
    {
        Ai = ABCD[4 * (io - 1)];
        Bi = ABCD[4 * (io - 1) + 1];
        Ci = 0;
        Di = 0;
    }*/
    double f = Ai * (m3 * m3) * exp(-m3 * zcalcul[ki - 1]) - m3 * (1 + 2 * MuCalcul[ki - 1] - m3 * zcalcul[ki - 1]) * Bi * exp(-m3 * zcalcul[ki - 1]) + (m3 * m3) * Ci * exp(m3 * zcalcul[ki - 1]) + m3 * (1 + 2 * MuCalcul[ki - 1] + m3 * zcalcul[ki - 1]) * Di * exp(m3 * zcalcul[ki - 1]);
    return f;
}

double fsigmar2(double m4)
{
    //double  Ai, Bi, Ci, Di;
    //vector<double>ABCD(k);
    //det_ABCD(m4, ABCD);
    if (io != nbrecouche)
    {
        Ai = ABCD[4 * (io - 1)];
        Bi = ABCD[4 * (io - 1) + 1];
        Ci = ABCD[4 * (io - 1) + 2];
        Di = ABCD[4 * (io - 1) + 3];
    }

    else
    {
        Ai = ABCD[4 * (io - 1)];
        Bi = ABCD[4 * (io - 1) + 1];
        Ci = 0;
        Di = 0;
    }
    double f = Ai * (m4 * m4) * exp(-m4 * zcalcul[ki - 1]) - m4 * (1 - m4 * zcalcul[ki - 1]) * Bi * exp(-m4 * zcalcul[ki - 1]) + (m4 * m4) * Ci * exp(m4 * zcalcul[ki - 1]) + m4 * (1 + m4 * zcalcul[ki - 1]) * Di * exp(m4 * zcalcul[ki - 1]);//ki != 1
    return (f); //ki != 1
}

double fsigmar02(double m5)
{
    //double  Ai, Bi, Ci, Di;
    //vector<double>ABCD(k);
    //det_ABCD(m5, ABCD);
    Ci = ABCD[4 * (io - 1) + 2];
    Di = ABCD[4 * (io - 1) + 3];
    double f = -1 + 2 * MuCalcul[ki - 1] + 4 * (m5 * m5) * (1 - MuCalcul[ki - 1]) * Ci + 8 * MuCalcul[ki - 1] * m5 * (1 - MuCalcul[ki - 1]) * Di;
    return (f);
}

double  fsigmaTeta1(double m6)
{
    //double  Ai, Bi, Ci, Di;
    //vector<double>ABCD(k);
    //det_ABCD(m6, ABCD);
    if (io != nbrecouche)
    {
        Ai = ABCD[4 * (io - 1)];
        Bi = ABCD[4 * (io - 1) + 1];
        Ci = ABCD[4 * (io - 1) + 2];
        Di = ABCD[4 * (io - 1) + 3];
    }
    else
    {
        Ai = ABCD[4 * (io - 1)];
        Bi = ABCD[4 * (io - 1) + 1];
        Ci = 0;
        Di = 0;
    }
    double f = -m6 * Bi * exp(-m6 * zcalcul[ki - 1]) * 2 * MuCalcul[ki - 1] + m6 * Di * exp(m6 * zcalcul[ki - 1]) * 2 * MuCalcul[ki - 1];//ki != 1
    return (f);
}
double fsigmaTeta2(double m7)
{
    //double  Ai, Bi, Ci, Di;
    //vector<double>ABCD(k);
    //det_ABCD(m7, ABCD);
    if (io != nbrecouche)
    {
        Ai = ABCD[4 * (io - 1)];
        Bi = ABCD[4 * (io - 1) + 1];
        Ci = ABCD[4 * (io - 1) + 2];
        Di = ABCD[4 * (io - 1) + 3];
    }
    else
    {
        Ai = ABCD[4 * (io - 1)];
        Bi = ABCD[4 * (io - 1) + 1];
        Ci = 0;
        Di = 0;
    }
    double f = m7 * m7 * Ai * exp(-m7 * zcalcul[ki - 1]) - m7 * (1 - m7 * zcalcul[ki - 1]) * Bi * exp(-m7 * zcalcul[ki - 1]) + m7 * m7 * Ci * exp(m7 * zcalcul[ki - 1]) + m7 * (1 + m7 * zcalcul[ki - 1]) * Di * exp(m7 * zcalcul[ki - 1]);//ki != 1
    return (f);
}
double fw(double m8)
{
    //double  Ai, Bi, Ci, Di;
    //vector<double>ABCD(k);
    //det_ABCD(m8, ABCD);
    if (io != nbrecouche)
    {
        Ai = ABCD[4 * (io - 1)];
        Bi = ABCD[4 * (io - 1) + 1];
        Ci = ABCD[4 * (io - 1) + 2];
        Di = ABCD[4 * (io - 1) + 3];
    }
    else
    {
        Ai = ABCD[4 * (io - 1)];
        Bi = ABCD[4 * (io - 1) + 1];
        Ci = 0;
        Di = 0;
    }
    double f = 1 - 2 * (m8 * m8) * Ci + 2 * m8 * (1 - 2 * MuCalcul[ki - 1]) * Di;
    return (f);
}
double fwi(double m9)
{
    //double  Ai, Bi, Ci, Di;
    //vector<double>ABCD(k);
    //det_ABCD(m9, ABCD);
    if (io != nbrecouche)
    {
        Ai = ABCD[4 * (io - 1)];
        Bi = ABCD[4 * (io - 1) + 1];
        Ci = ABCD[4 * (io - 1) + 2];
        Di = ABCD[4 * (io - 1) + 3];
    }
    else
    {
        Ai = ABCD[4 * (io - 1)];
        Bi = ABCD[4 * (io - 1) + 1];
        Ci = 0;
        Di = 0;
    }
    double f = -m9 * m9 * Ai * exp(-m9 * zcalcul[ki - 1]) - m9 * (2 - 4 * MuCalcul[ki - 1] + m9 * zcalcul[ki - 1]) * Bi * exp(-m9 * zcalcul[ki - 1]) + m9 * m9 * Ci * exp(m9 * zcalcul[ki - 1]) - m9 * (2 - 4 * MuCalcul[ki - 1] - m9 * zcalcul[ki - 1]) * Di * exp(m9 * zcalcul[ki - 1]);
    return (f);
}

//Arrondir les différents résultats au nombre de chiffres adéquats après la virgule
double Round(double x, int decimal)
{
    double multiplier = pow(10, decimal);
    return round(x * multiplier) / multiplier;
}

void calculsollicitations(vector<vector<double>>& SollicitationsFinales)
{
    //nbrecouche  // Nombre de couche de la chaussée y compris le sol en place
    cal_altitude(epais);//

    // Définition des épaisseurs de chaque couche
    mat_calcul(epais, 1, zcalcul);// Définition des altitudes au niveau de chaque interface

    // Coefficient de poisson des différentes couches
    mat_calcul(Mu, 2, MuCalcul);

    // Module de young de chaque couche
    mat_calcul(Young, 2, YoungCalcul);// Obtention du module de young au niveau de chaque interface


    // Nature des interfaces : "0 = Interfaces Collées" ;
    //                         "1 = Interfaces semi-collées" ;
    //                        "2 = Interfaces décollées"

    int iterate = 4;    // Nombre de pairs (xi, wi) obtenu pour la quadrature de gauss
    int infini = 70;  // L'infini considéré pour le calcul
    int iterate_surf = 4;
    int infini_surf = 70;

    int first_itf = 0; // Variable pour vérifier s'il y a une interface semi-collée
    int op_itf_semi_collee = 1;
    vector<vector<double>> tabInterface_i(2, vector<double>(nbrecouche - 1, 0));

    for (int i = 0; i < nbrecouche - 1; i++)
    {
        if (tabInterface[i] == 1)
        {
            for (int j = 0; j < nbrecouche - 1; j++)
            {
                tabInterface_i[0][j] = tabInterface[j];
                tabInterface_i[1][j] = tabInterface[j];
            }
            first_itf = 1;
            for (int itf = 0; itf < nbrecouche - 1; itf++)
            {
                if (tabInterface[itf] == 1)
                {
                    op_itf_semi_collee = 2;
                    tabInterface_i[0][itf] = 0;
                    tabInterface_i[1][itf] = 2;
                }
            }
        }
    }
    /* if (first_itf == 0 )
    {
        op_itf_semi_collee = 1 ; //Cas où il n'y a pas d'interface semi-collée
    }*/
    //roue: (input("Entrez la charge de référence R : Saisissez 1 pour roue isolée.Saisissez 2 pour roue jumelée")) //Variable définissant la charge de référence
                    //  roue = 1 : Roue isolée
                    //  roue = 2 : Jumelage standard français (roues jumelées)

    double rayon[3] = { 0 };
    int len;
    if (roue == 1)
    {
        //rayon[1] = 0;
        len = 1;
    }
    else
    {
        rayon[0] = 0;
        rayon[1] = d / 2;
        rayon[2] = d;
        len = 3;
    }

    //  if (first_itf != 0 )
      /*{*/   //Initialisation des matrices auxiliaires des sollicitations finales pour le cas où au moins une interface est semi-collée
    vector<double> SigZ_aux(2 * nbrecouche - 1, 0);
    vector<double> SigR_aux(2 * nbrecouche - 1, 0);
    vector<double> SigTeta_aux(2 * nbrecouche - 1, 0);
    vector<double> SigT_aux(2 * nbrecouche - 1, 0);
    vector<double> EpsiZ_aux(2 * nbrecouche - 1, 0);
    vector<double> EpsiT_aux(2 * nbrecouche - 1, 0);
    vector<double> w_aux(2 * nbrecouche - 1, 0);
    vector<double> w1_aux(2 * nbrecouche - 1, 0);

    vector<double> EpsiZ(2 * nbrecouche - 1, 0);
    vector<double> EpsiT(2 * nbrecouche - 1, 0);
    vector<double> EpsiR(2 * nbrecouche - 1, 0);
    vector<double> EpsiTeta(2 * nbrecouche - 1, 0);
    vector<double> SigZ(2 * nbrecouche - 1, 0);
    vector<double> SigR(2 * nbrecouche - 1, 0);
    vector<double> SigR1(2 * nbrecouche - 1, 0);
    vector<double> SigR2(2 * nbrecouche - 1, 0);
    vector<double> SigTeta(2 * nbrecouche - 1, 0);
    vector<double> SigTeta1(2 * nbrecouche - 1, 0);
    vector<double> SigTeta2(2 * nbrecouche - 1, 0);
    vector<double> SigT(2 * nbrecouche - 1, 0);
    vector<double> w(2 * nbrecouche - 1, 0);
    vector<double> w1(2 * nbrecouche - 1, 0);


    vector<vector<double>> SigZ_ri(len, vector<double>(2 * nbrecouche - 1, 0));
    vector<vector<double>> SigR_ri(len, vector<double>(2 * nbrecouche - 1, 0));
    vector<vector<double>> SigTeta_ri(len, vector<double>(2 * nbrecouche - 1, 0));
    vector<vector<double>> w_ri(len, vector<double>(2 * nbrecouche - 1, 0));
    vector<vector<double>> w1_ri(len, vector<double>(2 * nbrecouche - 1, 0));
    //Initilisation des matrices de contraintes issues des combinaisons de contraintes [Comb1 : cont(r=0) +cont(r=d) / Comb2 : 2xcont(r=d/2)]

    vector<double> SigZ1_3(2 * nbrecouche - 1, 0);
    vector<double> SigZ2x2(2 * nbrecouche - 1, 0);
    vector<double> SigR1_3(2 * nbrecouche - 1, 0);
    vector<double> SigR2x2(2 * nbrecouche - 1, 0);
    vector<double> SigTeta1_3(2 * nbrecouche - 1, 0);
    vector<double> SigTeta2x2(2 * nbrecouche - 1, 0);

    //Initialisation des matrices de déformations issues des combinaisons de contraintes
    vector<double> EpsiZ1_3(2 * nbrecouche - 1, 0);
    vector<double> EpsiZ2x2(2 * nbrecouche - 1, 0);
    vector<double> EpsiR1_3(2 * nbrecouche - 1, 0);
    vector<double> EpsiR2x2(2 * nbrecouche - 1, 0);
    vector<double> EpsiTeta1_3(2 * nbrecouche - 1, 0);
    vector<double> EpsiTeta2x2(2 * nbrecouche - 1, 0);
    vector<double> EpsiT1_3(2 * nbrecouche - 1, 0);
    vector<double> EpsiT2x2(2 * nbrecouche - 1, 0);

    //Initialisation des matrices de déplacements
    vector<double> w2x2(2 * nbrecouche - 1, 0);
    vector<double> w1_2x2(2 * nbrecouche - 1, 0);

    for (int op = 0; op < op_itf_semi_collee; op++)
    {
        if (first_itf != 0)
        {
            if (op == 0)
            {

                for (int i = 0; i < nbrecouche - 1; i++)
                {
                    tabInterface[i] = tabInterface_i[0][i];
                }
            }
            else if (op == 1)
            {
                for (int i = 0; i < nbrecouche - 1; i++)
                {
                    tabInterface[i] = tabInterface_i[1][i];
                }
            }
        }

        // ************************* Intégration numérique ****************************

        // Re-Initialisation des valeurs des matrices des sollicitations
        for (int i = 0; i < 2 * nbrecouche - 1; i++)
        {
            SigZ[i] = 0;
            SigR[i] = 0;
            SigR1[i] = 0;
            SigR2[i] = 0;
            SigTeta[i] = 0;
            SigTeta1[i] = 0;
            SigTeta2[i] = 0;
            w[i] = 0;
            w1[i] = 0;
        }

        for (int k = 0; k < len; k++)
        {
            double r = rayon[k];
            double r1;
            if (roue == 1)
            {
                r1 = 3 * a / 10;// r1 est choisi de façon forfaitaire par la relation ci-avant
                //r1 = a/2
                //r1 = d/2
            }
            else
            {
                //r1 = sqrt(2)*d/2 ;
                //r1 = sqrt((d/2)**2 + (a/2)**2) ;
                r1 = sqrt((d / 2) * (d / 2) + (a / 2) * (a / 2));
            }

            ki = 1; //Variable qui passe d'interface 1 à 2*nbrecouche-1 (Nous avous deux interfaces pour chaque couche sauf la dernière couche)
            for (io = 1; io < nbrecouche + 1; io++)
            {
                for (int j = 1; j < 3; j++)
                {
                    if (ki < 2 * nbrecouche)
                    {
                        if (ki != 1)  //Pour le calcul de SigZ et SigR au niveau de toutes les interfaces
                        {
                            vector<vector<double>> LegeGauss(iterate, vector<double>(2));
                            GaussianQuadratureWeights(iterate, 0, cyl_bessel_j_zero(0.0, 1), LegeGauss);
                            for (int ip = 1; ip < iterate + 1; ip++)
                            {
                                det_ABCD(LegeGauss[ip - 1][0], ABCD);
                                w[ki - 1] = w[ki - 1] + LegeGauss[ip - 1][1] * (fwi(LegeGauss[ip - 1][0]) * cyl_bessel_j(0, LegeGauss[ip - 1][0] * r) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * a) / LegeGauss[ip - 1][0]);
                                w1[ki - 1] = w1[ki - 1] + LegeGauss[ip - 1][1] * (fwi(LegeGauss[ip - 1][0]) * cyl_bessel_j(0, LegeGauss[ip - 1][0] * r1) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * a) / LegeGauss[ip - 1][0]);
                                SigZ[ki - 1] = SigZ[ki - 1] + LegeGauss[ip - 1][1] * (fsigmaz(LegeGauss[ip - 1][0]) * cyl_bessel_j(0, LegeGauss[ip - 1][0] * r) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * a));
                                SigR1[ki - 1] = SigR1[ki - 1] + LegeGauss[ip - 1][1] * (fsigmar1(LegeGauss[ip - 1][0]) * cyl_bessel_j(0, LegeGauss[ip - 1][0] * r) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * a));
                                if (roue != 1)
                                {
                                    SigTeta1[ki - 1] = SigTeta1[ki - 1] + LegeGauss[ip - 1][1] * (fsigmaTeta1(LegeGauss[ip - 1][0]) * cyl_bessel_j(0, LegeGauss[ip - 1][0] * r) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * a));
                                }

                                if (r == 0)
                                {
                                    SigR2[ki - 1] = SigR2[ki - 1] + LegeGauss[ip - 1][1] * (fsigmar2(LegeGauss[ip - 1][0]) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * a) * (cyl_bessel_j(0, LegeGauss[ip - 1][0] * r) - cyl_bessel_j(2, LegeGauss[ip - 1][0] * r))) / 2;
                                    if (roue != 1)
                                    {
                                        SigTeta2[ki - 1] = SigTeta2[ki - 1] + LegeGauss[ip - 1][1] * (fsigmaTeta2(LegeGauss[ip - 1][0]) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * a) * (cyl_bessel_j(0, LegeGauss[ip - 1][0] * r) - cyl_bessel_j(2, LegeGauss[ip - 1][0] * r))) / 2;
                                    }
                                }
                                if (r != 0)
                                {
                                    SigR2[ki - 1] = SigR2[ki - 1] + LegeGauss[ip - 1][1] * (fsigmar2(LegeGauss[ip - 1][0]) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * a) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * r)) / (LegeGauss[ip - 1][0] * r);
                                    if (roue != 1)
                                    {
                                        SigTeta2[ki - 1] = SigTeta2[ki - 1] + LegeGauss[ip - 1][1] * (fsigmaTeta2(LegeGauss[ip - 1][0]) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * a) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * r)) / (LegeGauss[ip - 1][0] * r);
                                    }
                                }
                            }
                            for (int zi = 1; zi < infini + 1; zi++)
                            {
                                GaussianQuadratureWeights(iterate, cyl_bessel_j_zero(0.0, zi), cyl_bessel_j_zero(0.0, zi + 1), LegeGauss);
                                for (int ip = 1; ip < iterate + 1; ip++)
                                {
                                    det_ABCD(LegeGauss[ip - 1][0], ABCD);
                                    w[ki - 1] = w[ki - 1] + LegeGauss[ip - 1][1] * (fwi(LegeGauss[ip - 1][0]) * cyl_bessel_j(0, LegeGauss[ip - 1][0] * r) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * a) / LegeGauss[ip - 1][0]);
                                    w1[ki - 1] = w1[ki - 1] + LegeGauss[ip - 1][1] * (fwi(LegeGauss[ip - 1][0]) * cyl_bessel_j(0, LegeGauss[ip - 1][0] * r1) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * a) / LegeGauss[ip - 1][0]);
                                    SigZ[ki - 1] = SigZ[ki - 1] + LegeGauss[ip - 1][1] * (fsigmaz(LegeGauss[ip - 1][0]) * cyl_bessel_j(0, LegeGauss[ip - 1][0] * r) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * a));
                                    SigR1[ki - 1] = SigR1[ki - 1] + LegeGauss[ip - 1][1] * (fsigmar1(LegeGauss[ip - 1][0]) * cyl_bessel_j(0, LegeGauss[ip - 1][0] * r) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * a));
                                    if (roue != 1)
                                    {
                                        SigTeta1[ki - 1] = SigTeta1[ki - 1] + LegeGauss[ip - 1][1] * (fsigmaTeta1(LegeGauss[ip - 1][0]) * cyl_bessel_j(0, LegeGauss[ip - 1][0] * r) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * a));
                                    }
                                    if (r == 0)
                                    {
                                        SigR2[ki - 1] = SigR2[ki - 1] + LegeGauss[ip - 1][1] * (fsigmar2(LegeGauss[ip - 1][0]) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * a) * (cyl_bessel_j(0, LegeGauss[ip - 1][0] * r) - cyl_bessel_j(2, LegeGauss[ip - 1][0] * r))) / 2;
                                        if (roue != 1)
                                        {
                                            SigTeta2[ki - 1] = SigTeta2[ki - 1] + LegeGauss[ip - 1][1] * (fsigmaTeta2(LegeGauss[ip - 1][0]) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * a) * (cyl_bessel_j(0, LegeGauss[ip - 1][0] * r) - cyl_bessel_j(2, LegeGauss[ip - 1][0] * r))) / 2;
                                        }
                                    }
                                    if (r != 0)
                                    {
                                        SigR2[ki - 1] = SigR2[ki - 1] + LegeGauss[ip - 1][1] * (fsigmar2(LegeGauss[ip - 1][0]) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * a) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * r)) / (LegeGauss[ip - 1][0] * r);
                                        if (roue != 1)
                                        {
                                            SigTeta2[ki - 1] = SigTeta2[ki - 1] + LegeGauss[ip - 1][1] * (fsigmaTeta2(LegeGauss[ip - 1][0]) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * a) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * r)) / (LegeGauss[ip - 1][0] * r);
                                        }
                                    }
                                }
                            }

                            SigZ[ki - 1] = Poids * a * SigZ[ki - 1];
                            w[ki - 1] = -100000 * Poids * a * w[ki - 1] * (1 + (MuCalcul[ki - 1])) / YoungCalcul[ki - 1];
                            w1[ki - 1] = -100000 * Poids * a * w1[ki - 1] * (1 + (MuCalcul[ki - 1])) / YoungCalcul[ki - 1];
                            SigR[ki - 1] = -Poids * a * (SigR1[ki - 1] - SigR2[ki - 1]);
                            if (roue != 1)
                            {
                                SigTeta[ki - 1] = -Poids * a * (SigTeta1[ki - 1] + SigTeta2[ki - 1]);
                            }
                        }
                        if (ki == 1) // Pour le calcul de SigZ et SigR à la surface de la 1ère couche
                        {
                            vector<vector<double>> LegeGauss(iterate_surf, vector<double>(2));
                            GaussianQuadratureWeights(iterate_surf, 0, cyl_bessel_j_zero(0.0, 1), LegeGauss);
                            for (int ip = 1; ip < iterate_surf + 1; ip++)
                            {
                                det_ABCD(LegeGauss[ip - 1][0], ABCD);
                                w[ki - 1] = w[ki - 1] + LegeGauss[ip - 1][1] * (fw(LegeGauss[ip - 1][0]) * cyl_bessel_j(0, LegeGauss[ip - 1][0] * r) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * a) / LegeGauss[ip - 1][0]);
                                w1[ki - 1] = w1[ki - 1] + LegeGauss[ip - 1][1] * (fw(LegeGauss[ip - 1][0]) * cyl_bessel_j(0, LegeGauss[ip - 1][0] * r1) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * a) / LegeGauss[ip - 1][0]);
                                SigR1[ki - 1] = SigR1[ki - 1] + LegeGauss[ip - 1][1] * (fsigmar1(LegeGauss[ip - 1][0]) * cyl_bessel_j(0, LegeGauss[ip - 1][0] * r) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * a));
                                if (roue != 1)
                                {
                                    SigTeta1[ki - 1] = SigTeta1[ki - 1] + LegeGauss[ip - 1][1] * (fsigmaTeta1(LegeGauss[ip - 1][0]) * cyl_bessel_j(0, LegeGauss[ip - 1][0] * r) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * a));
                                }
                                if (r == 0)
                                {
                                    SigR2[ki - 1] = SigR2[ki - 1] + LegeGauss[ip - 1][1] * (fsigmar2(LegeGauss[ip - 1][0]) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * a) * (cyl_bessel_j(0, LegeGauss[ip - 1][0] * r) - cyl_bessel_j(2, LegeGauss[ip - 1][0] * r))) / 2;
                                    if (roue != 1)
                                    {
                                        SigTeta2[ki - 1] = SigTeta2[ki - 1] + LegeGauss[ip - 1][1] * (fsigmaTeta2(LegeGauss[ip - 1][0]) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * a) * (cyl_bessel_j(0, LegeGauss[ip - 1][0] * r) - cyl_bessel_j(2, LegeGauss[ip - 1][0] * r))) / 2;
                                    }
                                }
                                if (r != 0)
                                {
                                    SigR2[ki - 1] = SigR2[ki - 1] + LegeGauss[ip - 1][1] * (fsigmar2(LegeGauss[ip - 1][0]) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * a) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * r)) / (LegeGauss[ip - 1][0] * r);
                                    if (roue != 1)
                                    {
                                        SigTeta2[ki - 1] = SigTeta2[ki - 1] + LegeGauss[ip - 1][1] * (fsigmaTeta2(LegeGauss[ip - 1][0]) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * a) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * r)) / (LegeGauss[ip - 1][0] * r);
                                    }
                                }
                            }
                            for (int zi = 1; zi < infini_surf + 1; zi++)
                            {
                                GaussianQuadratureWeights(iterate_surf, cyl_bessel_j_zero(0.0, zi), cyl_bessel_j_zero(0.0, zi + 1), LegeGauss);
                                for (int ip = 1; ip < iterate_surf + 1; ip++)
                                {
                                    det_ABCD(LegeGauss[ip - 1][0], ABCD);
                                    w[ki - 1] = w[ki - 1] + LegeGauss[ip - 1][1] * (fw(LegeGauss[ip - 1][0]) * cyl_bessel_j(0, LegeGauss[ip - 1][0] * r) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * a) / LegeGauss[ip - 1][0]);
                                    w1[ki - 1] = w1[ki - 1] + LegeGauss[ip - 1][1] * (fw(LegeGauss[ip - 1][0]) * cyl_bessel_j(0, LegeGauss[ip - 1][0] * r1) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * a) / LegeGauss[ip - 1][0]);
                                    SigR1[ki - 1] = SigR1[ki - 1] + LegeGauss[ip - 1][1] * (fsigmar1(LegeGauss[ip - 1][0]) * cyl_bessel_j(0, LegeGauss[ip - 1][0] * r) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * a));
                                    if (roue != 1)
                                    {
                                        SigTeta1[ki - 1] = SigTeta1[ki - 1] + LegeGauss[ip - 1][1] * (fsigmaTeta1(LegeGauss[ip - 1][0]) * cyl_bessel_j(0, LegeGauss[ip - 1][0] * r) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * a));
                                    }
                                    if (r == 0)
                                    {
                                        SigR2[ki - 1] = SigR2[ki - 1] + LegeGauss[ip - 1][1] * (fsigmar2(LegeGauss[ip - 1][0]) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * a) * (cyl_bessel_j(0, LegeGauss[ip - 1][0] * r) - cyl_bessel_j(2, LegeGauss[ip - 1][0] * r))) / 2;
                                        if (roue != 1)
                                        {
                                            SigTeta2[ki - 1] = SigTeta2[ki - 1] + LegeGauss[ip - 1][1] * (fsigmaTeta2(LegeGauss[ip - 1][0]) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * a) * (cyl_bessel_j(0, LegeGauss[ip - 1][0] * r) - cyl_bessel_j(2, LegeGauss[ip - 1][0] * r))) / 2;
                                        }
                                    }
                                    if (r != 0)
                                    {
                                        SigR2[ki - 1] = SigR2[ki - 1] + LegeGauss[ip - 1][1] * (fsigmar2(LegeGauss[ip - 1][0]) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * a) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * r)) / (LegeGauss[ip - 1][0] * r);
                                        if (roue != 1)
                                        {
                                            SigTeta2[ki - 1] = SigTeta2[ki - 1] + LegeGauss[ip - 1][1] * (fsigmaTeta2(LegeGauss[ip - 1][0]) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * a) * cyl_bessel_j(1, LegeGauss[ip - 1][0] * r)) / (LegeGauss[ip - 1][0] * r);
                                        }
                                    }
                                }
                            }
                            if (r < a)
                            {
                                SigZ[ki - 1] = Poids;
                                SigR[ki - 1] = -Poids * a * (SigR1[ki - 1] - SigR2[ki - 1]);
                                w[ki - 1] = 200000 * Poids * a * w[ki - 1] * (1 - pow((MuCalcul[ki - 1]), 2)) / YoungCalcul[ki - 1];
                                w1[ki - 1] = 200000 * Poids * a * w1[ki - 1] * (1 - pow((MuCalcul[ki - 1]), 2)) / YoungCalcul[ki - 1];
                                if (roue != 1)
                                {
                                    SigTeta[ki - 1] = -Poids * a * (SigTeta1[ki - 1] + SigTeta2[ki - 1]);
                                }
                            }
                            if (r > a)
                            {
                                SigZ[ki - 1] = 0;
                                SigR[ki - 1] = -Poids * a * (SigR1[ki - 1] - SigR2[ki - 1]);
                                w[ki - 1] = 200000 * Poids * a * w[ki - 1] * (1 - pow((MuCalcul[ki - 1]), 2)) / YoungCalcul[ki - 1];
                                w1[ki - 1] = 200000 * Poids * a * w1[ki - 1] * (1 - pow((MuCalcul[ki - 1]), 2)) / YoungCalcul[ki - 1];
                                if (roue != 1)
                                {
                                    SigTeta[ki - 1] = -Poids * a * (SigTeta1[ki - 1] + SigTeta2[ki - 1]);
                                }
                            }
                            if (r == a)
                            {
                                SigZ[ki - 1] = Poids / 2;
                                SigR[ki - 1] = -Poids * a * (SigR1[ki - 1] - SigR2[ki - 1]);
                                w[ki - 1] = 200000 * Poids * a * w[ki - 1] * (1 - pow((MuCalcul[ki - 1]), 2)) / YoungCalcul[ki - 1];
                                w1[ki - 1] = 200000 * Poids * a * w1[ki - 1] * (1 - pow((MuCalcul[ki - 1]), 2)) / YoungCalcul[ki - 1];
                                if (roue != 1)
                                {
                                    SigTeta[ki - 1] = -Poids * a * (SigTeta1[ki - 1] + SigTeta2[ki - 1]);
                                }
                            }
                        }
                        if (roue == 1)
                        {
                            EpsiZ[ki - 1] = (SigZ[ki - 1] * pow(10, 6) - 2 * pow(10, 6) * MuCalcul[ki - 1] * SigR[ki - 1]) / YoungCalcul[ki - 1];
                            EpsiT[ki - 1] = (SigR[ki - 1] * pow(10, 6) - pow(10, 6) * MuCalcul[ki - 1] * (SigZ[ki - 1] + SigR[ki - 1])) / YoungCalcul[ki - 1];
                        }
                        cout << w[ki - 1] << "    " << endl;
                        cout << w1[ki - 1] << "    " << endl;
                        cout << SigR1[ki - 1] << "    " << endl;
                        cout << SigR1[ki - 1] << "    " << endl;
                        cout << SigTeta1[ki - 1] << "    " << endl;
                        cout << SigTeta2[ki - 1] << "    " << endl;
                        cout << endl;
                        ki += 1;
                    }
                }
            }
            if (roue != 1)
            {

                // Affectation des sollicitations intermédiaires à la ligne correspondante des matrices SolX_ri
                for (int i = 0; i < 2 * nbrecouche - 1; i++)
                {
                    SigZ_ri[k][i] = SigZ[i];
                    SigR_ri[k][i] = SigR[i];
                    SigTeta_ri[k][i] = SigTeta[i];
                    w_ri[k][i] = w[i];
                    w1_ri[k][i] = w1[i];
                }

                // Re-Initialisation des valeurs des matrices SolX et SolXi
                for (int i = 0; i < 2 * nbrecouche - 1; i++)
                {
                    SigZ[i] = 0;
                    SigR[i] = 0;
                    SigR1[i] = 0;
                    SigR2[i] = 0;
                    SigTeta[i] = 0;
                    SigTeta1[i] = 0;
                    SigTeta2[i] = 0;
                    w[i] = 0;
                    w1[i] = 0;
                }

            }
        }
        if (roue != 1)
        {
            //Calcul des combinaisons de sollicitation dans le cas des roues jumelées
            for (int i = 0; i < 2 * nbrecouche - 1; i++)
            {
                SigZ1_3[i] = SigZ_ri[0][i] + SigZ_ri[2][i];
                SigZ2x2[i] = 2 * SigZ_ri[1][i];
                SigR1_3[i] = SigR_ri[0][i] + SigR_ri[2][i];
                SigR2x2[i] = 2 * SigR_ri[1][i];
                SigTeta1_3[i] = SigTeta_ri[0][i] + SigTeta_ri[2][i];
                SigTeta2x2[i] = 2 * SigTeta_ri[1][i];
                w2x2[i] = 2 * w_ri[1][i];
                w1_2x2[i] = 2 * w1_ri[1][i];
            }

            //Calcul des sollicitations critiques
            for (int i = 0; i < 2 * nbrecouche - 1; i++)
            {
                SigZ[i] = max(SigZ1_3[i], SigZ2x2[i]);
                SigR[i] = min(SigR1_3[i], SigR2x2[i]);
                SigTeta[i] = min(SigTeta1_3[i], SigTeta2x2[i]);
                SigT[i] = min(SigR[i], SigTeta[i]);

                EpsiZ1_3[i] = (SigZ1_3[i] * pow(10, 6) - pow(10, 6) * MuCalcul[i] * (SigR1_3[i] + SigTeta1_3[i])) / YoungCalcul[i];
                EpsiZ2x2[i] = (SigZ2x2[i] * pow(10, 6) - pow(10, 6) * MuCalcul[i] * (SigR2x2[i] + SigTeta2x2[i])) / YoungCalcul[i];
                EpsiR1_3[i] = (SigR1_3[i] * pow(10, 6) - pow(10, 6) * MuCalcul[i] * (SigZ1_3[i] + SigTeta1_3[i])) / YoungCalcul[i];
                EpsiR2x2[i] = (SigR2x2[i] * pow(10, 6) - pow(10, 6) * MuCalcul[i] * (SigZ2x2[i] + SigTeta2x2[i])) / YoungCalcul[i];
                EpsiTeta1_3[i] = (SigTeta1_3[i] * pow(10, 6) - pow(10, 6) * MuCalcul[i] * (SigZ1_3[i] + SigR1_3[i])) / YoungCalcul[i];
                EpsiTeta2x2[i] = (SigTeta2x2[i] * pow(10, 6) - pow(10, 6) * MuCalcul[i] * (SigZ2x2[i] + SigR2x2[i])) / YoungCalcul[i];

                EpsiZ[i] = max(EpsiZ1_3[i], EpsiZ2x2[i]);
                EpsiT[i] = min(EpsiR1_3[i], EpsiR2x2[i]);
                EpsiT[i] = min(EpsiT[i], EpsiTeta1_3[i]);
                EpsiT[i] = min(EpsiT[i], EpsiTeta2x2[i]);

                w[i] = w2x2[i];
                w1[i] = w1_2x2[i];
            }
        }

        if (first_itf != 0)
        {
            for (int i = 0; i < 2 * nbrecouche - 1; i++)
            {
                SigZ_aux[i] += SigZ[i] / 2;
                SigR_aux[i] += SigR[i] / 2;
                SigTeta_aux[i] += SigTeta[i] / 2;
                SigT_aux[i] += SigT[i] / 2;
                EpsiZ_aux[i] += EpsiZ[i] / 2;
                EpsiT_aux[i] += EpsiT[i] / 2;
                w_aux[i] += w[i] / 2;
                w1_aux[i] += w1[i] / 2;
            }
        }
    }
    if (first_itf != 0)
    {
        for (int i = 0; i < 2 * nbrecouche - 1; i++)
        {
            SigZ[i] = SigZ_aux[i];
            SigR[i] = SigR_aux[i];
            SigTeta[i] = SigTeta_aux[i];
            SigT[i] = SigT_aux[i];
            EpsiZ[i] = EpsiZ_aux[i];
            EpsiT[i] = EpsiT_aux[i];
            w[i] = w_aux[i];
            w1[i] = w1_aux[i];
        }
    }

    for (int i = 0; i < 2 * nbrecouche - 1; i++)
    {
        if (roue == 1)
        {
            SigR[i] = Round(SigR[i], 3);
        }
        else if (roue == 2)
        {
            SigT[i] = Round(SigT[i], 3);
        }
        EpsiT[i] = Round(EpsiT[i], 1);
        SigZ[i] = Round(SigZ[i], 3);
        EpsiZ[i] = Round(EpsiZ[i], 1);
        w[i] = Round(w[i], 2);
        // Courbure[i] = Round(Courbure[i], 2);
    }
    //Constitution de la grande matrice constenant les sollicitations à renvoyer
    if (roue == 1)
    {
        for (int i = 0; i < 2 * nbrecouche - 1; i++)
        {
            SollicitationsFinales[0][i] = SigR[i];
            SollicitationsFinales[1][i] = EpsiT[i];
            SollicitationsFinales[2][i] = SigZ[i];
            SollicitationsFinales[3][i] = EpsiZ[i];
            SollicitationsFinales[4][i] = w[i];
        }
    }
    else if (roue == 2)
    {
        for (int i = 0; i < 2 * nbrecouche - 1; i++)
        {
            SollicitationsFinales[0][i] = SigT[i];
            SollicitationsFinales[1][i] = EpsiT[i];
            SollicitationsFinales[2][i] = SigZ[i];
            SollicitationsFinales[3][i] = EpsiZ[i];
            SollicitationsFinales[4][i] = w[i];
        }
    }
}


int main()
{
    roue = 2;
    Poids = 0.662;
    a = 0.125;
    d = 0.375;
    Mu[0] = 0.35;
    Mu[1] = 0.35;
    Mu[2] = 0.25;
    Mu[3] = 0.35;
    Mu[4] = 0.35;
    Mu[5] = 0.35;
    Mu[6] = 0.35;

    Young[0] = 1180;
    Young[1] = 1000;
    Young[2] = 14000;
    Young[3] = 1200;
    Young[4] = 600;
    Young[5] = 200;
    Young[6] = 50;

    epais[0] = 0.05;
    epais[1] = 0.12;
    epais[2] = 0.20;
    epais[3] = 0.15;
    epais[4] = 0.30;
    epais[5] = 0.25;
    epais[6] = 10000000;

    tabInterface[0] = 0;
    tabInterface[1] = 2;
    tabInterface[2] = 1;
    tabInterface[3] = 0;
    tabInterface[4] = 2;
    tabInterface[5] = 0;

    vector<vector<double>>Sollicitationsfinales(5, vector<double>(2 * nbrecouche - 1, 0));

    cout << "hello" << endl;
    calculsollicitations(Sollicitationsfinales);

    cout << endl;
    cout << "Sigma T" << endl;
    cout << "[";
    for (int i = 0; i < 2 * nbrecouche - 1; i++)
    {
        cout << Sollicitationsfinales[0][i] << "  ";
    }
    cout << "]";
    cout << endl;
    cout << "Epsi T" << endl;
    cout << "[";
    for (int i = 0; i < 2 * nbrecouche - 1; i++)
    {
        cout << Sollicitationsfinales[1][i] << "  ";
    }
    cout << "]";
    cout << endl;
    cout << "Sigma Z" << endl;
    cout << "[";
    for (int i = 0; i < 2 * nbrecouche - 1; i++)
    {
        cout << Sollicitationsfinales[2][i] << "  ";
    }
    cout << "]";
    cout << endl;
    cout << "Epsi Z" << endl;
    cout << "[";
    for (int i = 0; i < 2 * nbrecouche - 1; i++)
    {
        cout << Sollicitationsfinales[3][i] << "  ";
    }
    cout << "]";
    cout << endl;

    cout << "Deflexion w" << endl;
    cout << "[";
    for (int i = 0; i < 2 * nbrecouche - 1; i++)
    {
        cout << Sollicitationsfinales[4][i] << "  ";
    }
    cout << "]";

    cout << endl;
    return 0;
}
