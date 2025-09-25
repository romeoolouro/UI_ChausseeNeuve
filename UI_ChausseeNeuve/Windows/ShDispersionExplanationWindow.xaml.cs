using System;
using System.Globalization;
using System.Windows;
using UI_ChausseeNeuve.ViewModels;

namespace UI_ChausseeNeuve.Windows
{
    /// <summary>
    /// Fen�tre d'explication de la dispersion d'�paisseur Sh selon NF P98-086
    /// Reproduit le comportement d'Aliz� lors du double-clic sur "standard"
    /// </summary>
    public partial class ShDispersionExplanationWindow : Window
    {
        /// <summary>
        /// Mat�riau pour lequel on explique la dispersion
        /// </summary>
        public MaterialItem Material { get; }

        /// <summary>
        /// Valeur Sh calcul�e (par d�faut selon le type de mat�riau)
        /// </summary>
        public double CalculatedShValue { get; private set; }

        /// <summary>
        /// Indique si l'utilisateur a appliqu� la valeur
        /// </summary>
        public bool ValueApplied { get; private set; }

        public ShDispersionExplanationWindow(MaterialItem material)
        {
            InitializeComponent();
            Material = material ?? throw new ArgumentNullException(nameof(material));
            
            // Calculer la valeur Sh par d�faut selon les r�gles
            CalculateShValue();
            
            // Mettre � jour l'affichage
            UpdateDisplay();
        }

        /// <summary>
        /// Calcule la valeur Sh par d�faut selon les r�gles NF P98-086 et le type de mat�riau
        /// </summary>
        private void CalculateShValue()
        {
            if (Material.Category != "MB") { CalculatedShValue = 0.015; return; }

            var lname = (Material.Name ?? string.Empty).ToLowerInvariant();
            if (lname.Contains("eb-gb")) CalculatedShValue = 0.030; // Graves bitumes
            else if (lname.Contains("eme")) CalculatedShValue = 0.025; // EME
            else CalculatedShValue = 0.025; // BBSG, BBME, autres
        }

        /// <summary>
        /// Met � jour l'affichage avec la valeur calcul�e par d�faut
        /// </summary>
        private void UpdateDisplay()
        {
            // Mettre � jour le titre avec le nom du mat�riau
            Title = $"Dispersion d'�paisseur standard - {Material.Name}";
            // Le texte explicatif est d�sormais statique dans le XAML; pas d'affichage Sh fixe ici.
        }

        /// <summary>
        /// Essaie de r�cup�rer une valeur Sh � partir de l'entr�e utilisateur (�paisseur BB en m)
        /// </summary>
        /// <returns>Sh calcul� si entr�e valide, sinon null</returns>
        private double? GetShFromCurrentInput()
        {
            try
            {
                var raw = ThicknessInput.Text?.Trim();
                if (string.IsNullOrWhiteSpace(raw)) return null;
                raw = raw.Replace(",", ".");
                if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double e))
                    return null;
                if (e < 0 || e > 2) return null; // borne de s�curit�
                return ComputeShFromThickness(e);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gestionnaire pour le bouton "Appliquer cette valeur"
        /// Applique en priorit� la valeur issue de l'entr�e utilisateur (si disponible), sinon la valeur par d�faut.
        /// </summary>
        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1) Prendre la valeur calcul�e depuis l'entr�e utilisateur si possible
                double appliedSh = GetShFromCurrentInput() ?? CalculatedShValue;

                // 2) Appliquer au mat�riau
                Material.Sh = appliedSh;
                Material.ShStatus = "filled";
                Material.NotifyPropertyChanged(nameof(Material.ShDisplay));

                ValueApplied = true;
                
                MessageBox.Show(
                    $"Valeur Sh appliqu�e avec succ�s !\n\n" +
                    $"Mat�riau : {Material.Name}\n" +
                    $"Valeur Sh : {appliedSh:F3}m\n\n" +
                    $"Cette valeur respecte les recommandations de la norme NF P98-086 " +
                    $"pour ce type de mat�riau bitumineux.",
                    "Application r�ussie",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur lors de l'application de la valeur :\n{ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Gestionnaire pour le bouton "Fermer"
        /// </summary>
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Calcul piecewise depuis une epaisseur utilisateur
        private static double ComputeShFromThickness(double e)
        {
            if (e <= 0.10) return 0.010; // palier bas
            if (e >= 0.15) return 0.025; // palier haut
            double t = (e - 0.10) / 0.05; // 0..1
            return 0.010 + t * (0.025 - 0.010);
        }

        private void OnComputeShClick(object sender, RoutedEventArgs e)
        {
            ComputeError.Visibility = Visibility.Collapsed;
            try
            {
                var val = GetShFromCurrentInput();
                if (!val.HasValue)
                {
                    ComputeError.Text = "Entr�e invalide. Exemple: 0.12 (m)";
                    ComputeError.Visibility = Visibility.Visible;
                    return;
                }

                // Afficher le r�sultat
                ComputedShText.Text = $"{val.Value:F3} m";
                var raw = ThicknessInput.Text?.Trim()?.Replace(",", ".");
                _ = double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double eValue);
                ComputedShExplain.Text = eValue <= 0.10 ? "Palier bas (<=0.10 m)" : (eValue >= 0.15 ? "Palier haut (>=0.15 m)" : "Zone lineaire (0.10..0.15 m)");
            }
            catch (Exception ex)
            {
                ComputeError.Text = ex.Message;
                ComputeError.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// M�thode statique pour ouvrir la fen�tre d'explication
        /// </summary>
        public static bool ShowExplanation(MaterialItem material, Window? owner = null)
        {
            try
            {
                var window = new ShDispersionExplanationWindow(material);
                if (owner != null)
                    window.Owner = owner;
                
                return window.ShowDialog() == true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur lors de l'ouverture de la fen�tre d'explication :\n{ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
        }
    }
}