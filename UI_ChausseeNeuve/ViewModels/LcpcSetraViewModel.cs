using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace UI_ChausseeNeuve.ViewModels
{
    /// <summary>
    /// ViewModel pour la fen�tre LcpcSetraWindow
    /// G�re les valeurs CAM du Guide LCPC-SETRA 1994 avec s�lection interactive
    /// </summary>
    public class LcpcSetraViewModel : INotifyPropertyChanged
    {
        #region Champs priv�s - CAM Trafics moyens et forts
        private double _camBitumineux = 0.8;
        private double _camBitumineuxMince = 1.0;
        private double _camBitumineuxStructures = 0.8;
        private double _camTraitesHydrauliques = 1.3;
        private double _camBeton = 1.3;
        private double _camGranulaires = 1.0;
        #endregion

        #region Champs priv�s - CAM Faibles trafics
        private double _camT5 = 0.4;
        private double _camT4 = 0.5;
        private double _camT3 = 0.7;
        private double _camT3Plus = 0.8;
        #endregion

        #region Propri�t�s publiques pour s�lection
        
        /// <summary>
        /// Valeur CAM s�lectionn�e par l'utilisateur
        /// </summary>
        public double SelectedCamValue { get; private set; }
        
        /// <summary>
        /// Type de mat�riau de la valeur s�lectionn�e
        /// </summary>
        public string SelectedMaterialType { get; private set; } = "";
        
        /// <summary>
        /// Indique si une valeur a �t� s�lectionn�e
        /// </summary>
        public bool HasSelectedValue { get; private set; }

        #endregion

        #region Commandes

        public ICommand SelectCamCommand { get; }

        #endregion

        #region Constructeur

        public LcpcSetraViewModel()
        {
            SelectCamCommand = new RelayCommand<object>(SelectCamValue);
        }

        #endregion

        #region Propri�t�s - CAM Trafics moyens et forts

        /// <summary>
        /// CAM pour bitumineux h>20cm (valeur par d�faut: 0.8)
        /// </summary>
        public double CamBitumineux
        {
            get => _camBitumineux;
            set
            {
                _camBitumineux = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// CAM pour bitumineux h<20cm (valeur par d�faut: 1.0)
        /// </summary>
        public double CamBitumineuxMince
        {
            get => _camBitumineuxMince;
            set
            {
                _camBitumineuxMince = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// CAM pour bitumineux structures mixtes et inverses (valeur par d�faut: 0.8)
        /// </summary>
        public double CamBitumineuxStructures
        {
            get => _camBitumineuxStructures;
            set
            {
                _camBitumineuxStructures = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// CAM pour mat�riaux trait�s aux liants hydrauliques (valeur par d�faut: 1.3)
        /// </summary>
        public double CamTraitesHydrauliques
        {
            get => _camTraitesHydrauliques;
            set
            {
                _camTraitesHydrauliques = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// CAM pour b�ton (valeur par d�faut: 1.3)
        /// </summary>
        public double CamBeton
        {
            get => _camBeton;
            set
            {
                _camBeton = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// CAM pour mat�riaux granulaires (sol, GNT) (valeur par d�faut: 1.0)
        /// </summary>
        public double CamGranulaires
        {
            get => _camGranulaires;
            set
            {
                _camGranulaires = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Propri�t�s - CAM Faibles trafics

        /// <summary>
        /// CAM pour classe de trafic T5 (valeur par d�faut: 0.4)
        /// </summary>
        public double CamT5
        {
            get => _camT5;
            set
            {
                _camT5 = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// CAM pour classe de trafic T4 (valeur par d�faut: 0.5)
        /// </summary>
        public double CamT4
        {
            get => _camT4;
            set
            {
                _camT4 = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// CAM pour classe de trafic T3 (valeur par d�faut: 0.7)
        /// </summary>
        public double CamT3
        {
            get => _camT3;
            set
            {
                _camT3 = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// CAM pour classe de trafic T3+ (valeur par d�faut: 0.8)
        /// </summary>
        public double CamT3Plus
        {
            get => _camT3Plus;
            set
            {
                _camT3Plus = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region M�thodes des commandes

        /// <summary>
        /// S�lectionne une valeur CAM pour application
        /// </summary>
        private void SelectCamValue(object? parameter)
        {
            if (parameter is string camType)
            {
                SelectedCamValue = camType switch
                {
                    "bitumineux" => CamBitumineux,
                    "bitumineux_mince" => CamBitumineuxMince,
                    "bitumineux_structures" => CamBitumineuxStructures,
                    "traites_hydrauliques" => CamTraitesHydrauliques,
                    "beton" => CamBeton,
                    "granulaires" => CamGranulaires,
                    "T5" => CamT5,
                    "T4" => CamT4,
                    "T3" => CamT3,
                    "T3+" => CamT3Plus,
                    _ => 1.0
                };

                SelectedMaterialType = camType;
                HasSelectedValue = true;

                // D�clencher la s�lection de couche
                OnCamValueSelected?.Invoke(SelectedCamValue, GetDisplayNameForMaterialType(camType));
            }
        }

        /// <summary>
        /// Obtient le nom d'affichage pour un type de mat�riau
        /// </summary>
        private string GetDisplayNameForMaterialType(string materialType)
        {
            return materialType switch
            {
                "bitumineux" => "Bitumineux h>20cm",
                "bitumineux_mince" => "Bitumineux h<20cm", 
                "bitumineux_structures" => "Bitumineux structures mixtes",
                "traites_hydrauliques" => "Trait�s liants hydrauliques",
                "beton" => "B�ton",
                "granulaires" => "Granulaires (sol, GNT)",
                "T5" => "Classe T5",
                "T4" => "Classe T4", 
                "T3" => "Classe T3",
                "T3+" => "Classe T3+",
                _ => "Type inconnu"
            };
        }

        #endregion

        #region �v�nements

        /// <summary>
        /// �v�nement d�clench� quand une valeur CAM est s�lectionn�e
        /// </summary>
        public event System.Action<double, string>? OnCamValueSelected;

        #endregion

        #region M�thodes publiques

        /// <summary>
        /// Retourne les valeurs CAM pour trafics moyens et forts selon le type de mat�riau
        /// </summary>
        public double GetCamForMaterial(string materialType)
        {
            return materialType.ToLowerInvariant() switch
            {
                "bitumineux" => CamBitumineux,
                "bitumineux_mince" => CamBitumineuxMince,
                "bitumineux_structures" => CamBitumineuxStructures,
                "traites_hydrauliques" => CamTraitesHydrauliques,
                "beton" => CamBeton,
                "granulaires" => CamGranulaires,
                _ => 1.0 // Valeur par d�faut
            };
        }

        /// <summary>
        /// Retourne les valeurs CAM pour faibles trafics selon la classe
        /// </summary>
        public double GetCamForTrafficClass(string trafficClass)
        {
            return trafficClass.ToUpperInvariant() switch
            {
                "T5" => CamT5,
                "T4" => CamT4,
                "T3" => CamT3,
                "T3+" => CamT3Plus,
                _ => 1.0 // Valeur par d�faut
            };
        }

        /// <summary>
        /// R�initialise toutes les valeurs aux valeurs par d�faut du Guide LCPC-SETRA 1994
        /// </summary>
        public void ResetToDefaults()
        {
            // CAM Trafics moyens et forts
            CamBitumineux = 0.8;
            CamBitumineuxMince = 1.0;
            CamBitumineuxStructures = 0.8;
            CamTraitesHydrauliques = 1.3;
            CamBeton = 1.3;
            CamGranulaires = 1.0;

            // CAM Faibles trafics
            CamT5 = 0.4;
            CamT4 = 0.5;
            CamT3 = 0.7;
            CamT3Plus = 0.8;
        }

        #endregion

        #region INotifyPropertyChanged
        
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        #endregion
    }
}