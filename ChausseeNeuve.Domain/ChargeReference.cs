using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ChausseeNeuve.Domain.Models
{
    /// <summary>
    /// Représente une charge de référence selon la norme NF P98-086
    /// </summary>
    public class ChargeReference : INotifyPropertyChanged
    {
        private ChargeType _type;
        private double _rayonMetres = 0.125; // Rayon en mètres
        private double _pressionMPa = 0.662; // Pression en MPa
        private double _poidsMN = 0.0325; // Poids en MN (méganewtons)
        private double _distanceRouesMetres = 0.375; // Distance entre roues en mètres
        private double _positionX = 0; // Position X en mètres
        private double _positionY = 0; // Position Y en mètres

        public ChargeType Type
        {
            get => _type;
            set
            {
                if (_type != value)
                {
                    _type = value;
                    OnPropertyChanged();
                    // Réinitialiser les valeurs par défaut selon le type
                    SetDefaultValuesForType(value);
                }
            }
        }

        public double RayonMetres
        {
            get => _rayonMetres;
            set
            {
                if (_rayonMetres != value)
                {
                    _rayonMetres = Math.Max(0, value); // Valeur positive uniquement
                    OnPropertyChanged();
                }
            }
        }

        public double PressionMPa
        {
            get => _pressionMPa;
            set
            {
                if (_pressionMPa != value)
                {
                    _pressionMPa = Math.Max(0, value); // Valeur positive uniquement
                    OnPropertyChanged();
                }
            }
        }

        public double PoidsMN
        {
            get => _poidsMN;
            set
            {
                if (_poidsMN != value)
                {
                    _poidsMN = Math.Max(0, value); // Valeur positive uniquement
                    OnPropertyChanged();
                }
            }
        }

        public double DistanceRouesMetres
        {
            get => _distanceRouesMetres;
            set
            {
                if (_distanceRouesMetres != value)
                {
                    _distanceRouesMetres = Math.Max(0, value); // Valeur positive uniquement
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PositionYDisplay)); // Mettre à jour l'affichage Y qui dépend de la distance
                }
            }
        }

        public double PositionX
        {
            get => _positionX;
            set
            {
                if (_positionX != value)
                {
                    _positionX = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PositionXDisplay));
                }
            }
        }

        public double PositionY
        {
            get => _positionY;
            set
            {
                if (_positionY != value)
                {
                    _positionY = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PositionYDisplay));
                }
            }
        }

        // Propriétés d'affichage pour X et Y
        public string PositionXDisplay => _positionX == 0 ? "0" : _positionX.ToString("F3");

        public string PositionYDisplay
        {
            get
            {
                if (Type == ChargeType.RoueIsolee)
                {
                    return _positionY == 0 ? "0" : _positionY.ToString("F3");
                }
                // Pour les jumelages, afficher "0 et d/2"
                return "0 et d/2";
            }
        }

        /// <summary>
        /// Définit les valeurs par défaut selon le type de charge
        /// </summary>
        private void SetDefaultValuesForType(ChargeType type)
        {
            switch (type)
            {
                case ChargeType.JumelageFrancais:
                    RayonMetres = 0.125;
                    PressionMPa = 0.662;
                    PoidsMN = 0.0325;
                    DistanceRouesMetres = 0.375;
                    PositionX = 0;
                    PositionY = 0;
                    break;

                case ChargeType.AutreJumelage:
                    RayonMetres = 0.125;
                    PressionMPa = 0.662;
                    PoidsMN = 0.0325;
                    DistanceRouesMetres = 0.375;
                    PositionX = 0;
                    PositionY = 0;
                    break;

                case ChargeType.RoueIsolee:
                    RayonMetres = 0.125;
                    PressionMPa = 0.662;
                    PoidsMN = 0.0325;
                    DistanceRouesMetres = 0; // Non applicable pour roue isolée
                    PositionX = 0;
                    PositionY = 0;
                    break;
            }
            // Notifier les changements d'affichage
            OnPropertyChanged(nameof(PositionXDisplay));
            OnPropertyChanged(nameof(PositionYDisplay));
        }

        /// <summary>
        /// Indique si la distance entre roues est applicable pour ce type de charge
        /// </summary>
        public bool IsDistanceRouesApplicable => Type != ChargeType.RoueIsolee;

        /// <summary>
        /// Crée une copie de cette charge de référence
        /// </summary>
        public ChargeReference Clone()
        {
            return new ChargeReference
            {
                Type = this.Type,
                RayonMetres = this.RayonMetres,
                PressionMPa = this.PressionMPa,
                PoidsMN = this.PoidsMN,
                DistanceRouesMetres = this.DistanceRouesMetres,
                PositionX = this.PositionX,
                PositionY = this.PositionY
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
