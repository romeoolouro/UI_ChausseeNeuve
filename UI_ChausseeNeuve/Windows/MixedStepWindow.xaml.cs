using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using UI_ChausseeNeuve.ViewModels;

namespace UI_ChausseeNeuve.Windows
{
    public partial class MixedStepWindow : Window
    {
        private static readonly CultureInfo Fr = new CultureInfo("fr-FR");

        public MixedStepWindow()
        {
            InitializeComponent();
        }

        private static string FormatLargeNE(double ne)
        {
            if (ne <= 0 || double.IsNaN(ne) || double.IsInfinity(ne)) return "-";
            if (ne >= 1_000_000_000)
                return string.Format(Fr, "{0:N2} milliards", ne / 1_000_000_000.0);
            if (ne >= 1_000_000)
                return string.Format(Fr, "{0:N2} millions", ne / 1_000_000.0);
            return ne.ToString("0.###E+0", Fr);
        }

        private static string FormatLargeNPL(double npl)
        {
            if (npl <= 0 || double.IsNaN(npl) || double.IsInfinity(npl)) return "-";
            if (npl >= 1_000_000_000)
                return string.Format(Fr, "{0:N2} milliards", npl / 1_000_000_000.0);
            if (npl >= 1_000_000)
                return string.Format(Fr, "{0:N2} millions", npl / 1_000_000.0);
            return npl.ToString("N0", Fr);
        }

        private static string Prec(double v) => v.ToString("G9", Fr);

        private static string BuildSigmaTFormula(MixedVerificationDetails d)
        {
            var sb = new StringBuilder();
            sb.Append("NE = 10^6 x ( |sigT| / (sig6 x kc x kr x ks x kd) )^(-B)  =  ");
            var exp = -d.BFondation;
            sb.AppendFormat(Fr, "10^6 x ( {0} / ({1} x {2} x {3} x {4} x {5}) )^({6})",
                Prec(d.SigmaTBase), Prec(d.Sigma6Fondation), Prec(d.KcFondation), Prec(d.KrFondation), Prec(d.KsFondation), Prec(d.KdFondation), Prec(exp));
            return sb.ToString();
        }

        private static string BuildEpsiTFormula(MixedVerificationDetails d)
        {
            var sb = new StringBuilder();
            // Eviter les caracteres grecs pour compatibilite police: µ pour micro, kt pour k-theta
            sb.Append("NE = 10^6 x ( epsT / (eps6 x kc x kr x ks x kt) )^(-B)  =  ");
            var exp = -d.BBase;
            sb.AppendFormat(Fr, "10^6 x ( {0} / ({1} x {2} x {3} x {4} x {5}) )^({6})",
                Prec(d.EpsiTBaseResidual), Prec(d.Epsilon6Base), Prec(d.KcBase), Prec(d.KrBase), Prec(d.KsBase), Prec(d.KthetaBase), Prec(exp));
            return sb.ToString();
        }

        public void SetStep1(MixedVerificationDetails d)
        {
            StepTitle.Text = "Etape 1 - Fondation MTLH (sigT a la base)";
            LayerName.Text = d.FondationName;
            LayerOrder.Text = d.FondationOrder.ToString(Fr);
            Solicitation.Text = string.Format(Fr, "sigT,base = {0} MPa", Prec(d.SigmaTBase));
            FatigueParams.Text = string.Format(Fr, "sigma6={0} MPa, B={1}, CAM={2}, kc={3}, kr={4}, ks={5}, kd={6}",
                Prec(d.Sigma6Fondation), Prec(d.BFondation), Prec(d.CAMFondation), Prec(d.KcFondation), Prec(d.KrFondation), Prec(d.KsFondation), Prec(d.KdFondation));
            NEValue.Text = string.Format(Fr, "{0}  (={1})", FormatLargeNE(d.NE1), d.NE1.ToString("0.###E+0", Fr));
            Formula.Text = BuildSigmaTFormula(d);
            NPLValue.Text = string.Format(Fr, "{0}  (={1})", FormatLargeNPL(d.NPL1), d.NPL1.ToString("0.###E+0", Fr));
            Remarks.Text = "Calcul inverse via sigT et loi fatigue materiaux traites";
            RowsList.ItemsSource = null;

            ResidualResultsTable.ItemsSource = null;
            ResidualResultsSection.Visibility = Visibility.Collapsed;
        }

        public void SetStep2(MixedVerificationDetails d)
        {
            StepTitle.Text = "Etape 2 - Base MB (epsT residuel a la base)";
            LayerName.Text = d.BaseName;
            LayerOrder.Text = d.BaseOrder.ToString(Fr);
            // Utiliser \u00B5 pour micro (µ) afin d'eviter l'affichage '?'
            Solicitation.Text = string.Format(Fr, "epsT,base (residuel) = {0} \u00B5def", Prec(d.EpsiTBaseResidual));
            // Remplacer k? par kt (ascii) pour compatibilite police
            FatigueParams.Text = string.Format(Fr, "eps6={0} \u00B5def, B={1}, CAM={2}, kc={3}, kr={4}, ks={5}, kt={6}",
                Prec(d.Epsilon6Base), Prec(d.BBase), Prec(d.CAMBase), Prec(d.KcBase), Prec(d.KrBase), Prec(d.KsBase), Prec(d.KthetaBase));
            NEValue.Text = string.Format(Fr, "{0}  (={1})", FormatLargeNE(d.NE2), d.NE2.ToString("0.###E+0", Fr));
            Formula.Text = BuildEpsiTFormula(d);
            NPLValue.Text = string.Format(Fr, "{0}  (={1})", FormatLargeNPL(d.NPL2), d.NPL2.ToString("0.###E+0", Fr));

            var remarks = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(d.ResidualInfo)) remarks.Append(d.ResidualInfo);
            if (!string.IsNullOrWhiteSpace(d.RemodelingDetails))
            {
                if (remarks.Length > 0) remarks.Append("; ");
                remarks.Append(d.RemodelingDetails.Trim());
            }
            if (remarks.Length == 0) remarks.Append("Interface glissante; E(MTLH)=E/5");
            Remarks.Text = remarks.ToString();

            RowsList.ItemsSource = d.ModeledRows ?? Enumerable.Empty<MixedModeledRowDto>();

            // Remplir le tableau des résultats recalculés (visible uniquement s'il y a des lignes)
            var results = d.ResidualResults ?? Enumerable.Empty<ResultatCouche>();
            ResidualResultsTable.ItemsSource = results;
            ResidualResultsSection.Visibility = results.Any() ? Visibility.Visible : Visibility.Collapsed;
        }

        public void SetSynthesis(MixedVerificationDetails d)
        {
            StepTitle.Text = "Synthese - Trafic admissible";
            LayerName.Text = "-";
            LayerOrder.Text = "-";
            Solicitation.Text = "N/A";
            FatigueParams.Text = string.Format(Fr, "NPL cible = {0}", FormatLargeNPL(d.TargetNPL));
            NEValue.Text = "-";
            Formula.Text = "-";
            NPLValue.Text = string.Format(Fr, "NPL1 + NPL2 = {0}  (={1})", FormatLargeNPL(d.TotalNPL), d.TotalNPL.ToString("0.###E+0", Fr));
            Remarks.Text = d.IsOk ? "Conformite : la structure supporte le trafic." : "Non-conformite : la structure ne supporte pas le trafic.";
            RowsList.ItemsSource = null;

            ResidualResultsTable.ItemsSource = null;
            ResidualResultsSection.Visibility = Visibility.Collapsed;
        }

        private void OnClose(object sender, RoutedEventArgs e) => Close();
    }
}
