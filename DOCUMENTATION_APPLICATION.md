# Documentation Technique - Application UI_ChausseeNeuve

## Vue d'ensemble

L'application `UI_ChausseeNeuve` constitue l'interface utilisateur complète pour la conception et la validation de structures de chaussée selon la norme française NF P98-086. Elle implémente une architecture WPF MVVM moderne avec séparation claire des responsabilités.

## Architecture générale

### Structure du projet
```
UI_ChausseeNeuve/
├── App.xaml & App.xaml.cs          # Application principale
├── AppState.cs                     # État global de l'application
├── UI_ChausseeNeuve.csproj        # Configuration .NET 8.0 WPF
├── Converters/                     # Convertisseurs de données WPF
│   ├── StructureConverters.cs     # Convertisseurs métier
│   └── WidthToVisibilityConverter.cs
├── Resources/                      # Ressources XAML et médias
│   ├── Theme.xaml                 # Thème et styles globaux
│   └── Icons/                     # Icônes de l'application
├── Services/                       # Services applicatifs
│   ├── ProjectStorage.cs          # Persistance des projets
│   ├── RecentFiles.cs             # Gestion fichiers récents
│   ├── RelayCommand.cs            # Pattern commande MVVM
│   └── ToastService.cs            # Notifications utilisateur
├── ViewModels/                     # Modèles de vue MVVM
│   └── StructureEditorViewModel.cs # Éditeur de structure
└── Views/                          # Vues utilisateur
    ├── StructureDeChausseeView.xaml & .xaml.cs # Éditeur principal
    ├── RowTemplateSelector.cs      # Sélecteur de templates
    ├── FileMenuView.xaml           # Menu fichiers
    └── HoverNavBar.xaml            # Barre navigation
```

### Technologies utilisées
- **Framework**: .NET 8.0 avec WPF (Windows Presentation Foundation)
- **Paradigme**: MVVM (Model-View-ViewModel)
- **Binding**: Data Binding bidirectionnel
- **Styles**: Ressources XAML avec thèmes personnalisés
- **Animations**: Storyboards WPF pour transitions fluides
- **Validation**: INotifyDataErrorInfo avec feedback utilisateur

### Communication inter-couches

#### Flux de données principal
```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   View (XAML)   │◄──►│ ViewModel (C#)   │◄──►│ Domain (C#)     │
│                 │    │                  │    │                 │
│ • Data Binding  │    │ • INotifyProperty│    │ • Validation    │
│ • Commands      │    │ • RelayCommand   │    │ • Business Rules│
│ • Templates     │    │ • ObservableColl │    │ • NF P98-086    │
│ • Converters    │    │ • Events         │    │ • Calculations  │
└─────────────────┘    └──────────────────┘    └─────────────────┘
```

## États et gestion globale

### AppState.cs - État global

#### Responsabilités
- Gestion du projet courant
- Chemin du fichier ouvert
- État partagé entre fenêtres

#### Propriétés
```csharp
public static Project CurrentProject { get; set; }  // Projet en cours d'édition
public static string? CurrentFilePath { get; set; } // Chemin du fichier sauvegardé
```

#### Utilisation
```csharp
// Chargement d'un projet
AppState.CurrentProject = ProjectStorage.Load(filePath);
AppState.CurrentFilePath = filePath;

// Accès depuis ViewModels
var currentStructure = AppState.CurrentProject.PavementStructure;
```

## Services applicatifs

### 1. ProjectStorage.cs - Persistance

#### Fonctionnalités
- **Sérialisation JSON** des projets avec indentation
- **Désérialisation** avec gestion de compatibilité ascendante
- **Gestion des chemins** et création automatique des dossiers
- **Intégration RecentFiles** automatique

#### Configuration JSON
```csharp
private static readonly JsonSerializerOptions _opts = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    IncludeFields = false
};
```

#### Méthodes principales
```csharp
public static void Save(Project project, string path)    // Sauvegarde projet
public static Project Load(string path)                  // Chargement projet
```

#### Gestion de compatibilité
```csharp
// Assure la compatibilité avec anciens projets sans PavementStructure
if (proj.PavementStructure == null)
{
    proj.PavementStructure = new PavementStructure();
}
```

### 2. ToastService.cs - Notifications utilisateur

#### Architecture
- **Conteneur partagé** pour afficher les toasts
- **Animations WPF** pour entrées/sorties fluides
- **Types de notifications** (Success, Warning, Error, Info)
- **Auto-suppression** après délai configurable

#### Initialisation
```csharp
// Dans la vue principale
ToastService.Initialize(toastContainerPanel);
```

#### Utilisation
```csharp
// Depuis le domaine (injection de dépendance)
Layer.NotifyToast = (message, type) =>
    ToastService.ShowToast(message, type);

// Depuis ViewModels
ToastService.ShowToast("Validation réussie", ToastType.Success);
```

#### Styles par type
- **Success**: Fond vert avec icône ✅
- **Warning**: Fond jaune avec icône ⚠️
- **Error**: Fond rouge avec icône ❌
- **Info**: Fond bleu avec icône ℹ️

### 3. RelayCommand.cs - Pattern commande MVVM

#### Implémentation générique
```csharp
public class RelayCommand<T> : ICommand
{
    private readonly Action<T> _execute;
    private readonly Predicate<T>? _canExecute;

    public void Execute(object parameter) => _execute((T)parameter);
    public bool CanExecute(object parameter) => _canExecute?.Invoke((T)parameter) ?? true;
}
```

#### Utilisation typique
```csharp
// Dans ViewModel
AddLayerCommand = new RelayCommand(AddLayer);
DeleteLayerCommand = new RelayCommand<Layer>(DeleteLayer, l => l != null);
```

## ViewModels - Logique métier

### StructureEditorViewModel.cs - Éditeur principal

#### Responsabilités principales
- **Gestion de la collection** de couches (ObservableCollection)
- **Validation NF P98-086** complète avec auto-correction
- **Calculs temps réel** des coefficients Ks/Kd
- **Gestion de l'échelle** et graduations
- **Commandes utilisateur** (ajout, suppression, validation)
- **Communication avec domaine** via AppState

#### Propriétés clés

##### Collections observables
```csharp
public ObservableCollection<Layer> Layers { get; }           // Couches de la structure
public ObservableCollection<RowVM> Rows { get; }             // Lignes pour affichage (couches + interfaces)
public ObservableCollection<string> Errors { get; }          // Erreurs de validation
public ObservableCollection<string> Warnings { get; }        // Avertissements
```

##### Paramètres de visualisation
```csharp
public double DepthScale { get; set; }                       // Échelle profondeur/hauteur
public bool AutoScaleEnabled { get; set; }                   // Échelle automatique
public double ViewportHeight { get; set; }                   // Hauteur viewport
public ObservableCollection<GradItem> GradItems { get; }     // Graduations échelle
```

##### Métadonnées structure
```csharp
public double NE { get; set; }                               // Nombre équivalent d'essieux
public string SelectedStructureType { get; set; }            // Type de structure
public IReadOnlyList<string> StructureTypes { get; }         // Liste types disponibles
```

#### Commandes utilisateur
```csharp
public RelayCommand AddLayerCommand { get; }                 // Ajouter couche
public RelayCommand RemoveTopLayerCommand { get; }           // Supprimer couche supérieure
public RelayCommand<Layer> DeleteLayerCommand { get; }       // Supprimer couche spécifique
public RelayCommand ValidateStructureCommand { get; }       // Validation complète
```

#### Classes auxiliaires

##### RowVM - Hiérarchie d'affichage
```csharp
public abstract class RowVM { }                               // Classe de base

public class LayerRowVM : RowVM                              // Représente une couche
{
    public Layer Layer { get; }
    public IEnumerable<MaterialFamily> AvailableMaterials { get; }
}

public class InterfaceRowVM : RowVM                           // Représente une interface
{
    public Layer UpperLayer { get; }
}
```

##### GradItem - Échelle de profondeur
```csharp
public class GradItem
{
    public double Pixel { get; set; }                        // Position en pixels
    public string Label { get; set; }                        // Label (ex: "0.25 m")
}
```

#### Validation NF P98-086

##### Validation automatique des propriétés
- **Épaisseur**: Selon famille de matériau et rôle
- **Module**: Plages par matériau selon norme
- **Poisson**: Valeurs normalisées par matériau
- **Interfaces**: Règles selon NF P98-086 §8.5.1.3

##### Validation par type de structure
- **Souple**: Épaisseurs GNT ≥ 0.15m, BB ≤ 0.12m
- **Semi-rigide**: Roulement BB ≥ 0.06m, assises MTLH
- **Bitumineuse épaisse**: Ratio BB/total ∈ [0.45, 0.60]
- **Rigide**: Couche béton ≥ 0.12m

##### Auto-correction intelligente
```csharp
// Exemple: ajustement automatique des interfaces
if (upperLayer.InterfaceWithBelow != expectedInterface)
{
    upperLayer.InterfaceWithBelow = expectedInterface;
    warnings.Add($"Interface ajustée à '{expectedInterface}' (NF P98-086 §8.5.1.3)");
}
```

#### Calculs temps réel

##### Coefficients moyens
```csharp
var avgKs = Layers.Where(l => l.Role != LayerRole.Plateforme)
                  .Average(l => l.CoeffKs);
var avgKd = Layers.Where(l => l.Role != LayerRole.Plateforme)
                  .Average(l => l.CoeffKd);
```

##### Échelle automatique
```csharp
double total = Layers.Where(l => l.Role != LayerRole.Plateforme)
                     .Sum(l => l.Thickness_m);
double scale = usable / total;
if (scale > 0 && !double.IsInfinity(scale)) DepthScale = scale;
```

#### Événements et communication

##### Notifications toast
```csharp
public event Action<string, ToastType>? ToastRequested;

// Connexion avec le domaine
Layer.NotifyToast = (message, type) => ToastRequested?.Invoke(message, type);
```

##### Synchronisation AppState
```csharp
private void UpdateAppState()
{
    var pavementStructure = AppState.CurrentProject.PavementStructure;
    pavementStructure.Layers.Clear();
    foreach (var layer in Layers)
        pavementStructure.Layers.Add(layer);
    pavementStructure.NE = _ne;
    pavementStructure.StructureType = _selectedStructureType;
}
```

## Vues utilisateur (Views)

### StructureDeChausseeView.xaml - Éditeur principal

#### Architecture XAML
- **UserControl** pour intégration flexible
- **Data Binding bidirectionnel** avec ViewModel
- **Templates dynamiques** via RowTemplateSelector
- **Converters spécialisés** pour transformation données
- **Styles et ressources** centralisés

#### Structure principale
```xaml
<UserControl>
    <UserControl.Resources>
        <!-- Converters, styles, templates -->
    </UserControl.Resources>

    <Grid>
        <!-- Header avec contrôles -->
        <!-- Zone d'édition avec ItemsControl -->
        <!-- Graduations et échelle -->
        <!-- Boutons d'action -->
    </Grid>
</UserControl>
```

#### Templates de données

##### LayerTemplate - Affichage couche
```xaml
<DataTemplate x:Key="LayerTemplate">
    <Border Style="{StaticResource LayerCard}">
        <Grid>
            <!-- Informations couche -->
            <!-- Éditeurs propriétés -->
            <!-- Indicateurs validation -->
        </Grid>
    </Border>
</DataTemplate>
```

##### InterfaceTemplate - Affichage interface
```xaml
<DataTemplate x:Key="InterfaceTemplate">
    <Border Style="{StaticResource InterfaceCard}">
        <!-- Sélecteur type interface -->
    </Border>
</DataTemplate>
```

#### RowTemplateSelector.cs
```csharp
public class RowTemplateSelector : DataTemplateSelector
{
    public DataTemplate? LayerTemplate { get; set; }
    public DataTemplate? InterfaceTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        return item switch
        {
            LayerRowVM => LayerTemplate,
            InterfaceRowVM => InterfaceTemplate,
            _ => base.SelectTemplate(item, container)
        };
    }
}
```

### Fenêtres principales

#### ModeSelectionWindow.xaml
- **Sélection du mode** de dimensionnement (Expert/Automatique)
- **Navigation** vers éditeur approprié
- **Configuration initiale** du projet

#### AccueilWindow.xaml
- **Page d'accueil** avec menu principal
- **Navigation** vers différentes fonctionnalités
- **Affichage projets récents**

## Convertisseurs de données

### StructureConverters.cs

#### EqualityConverter
```csharp
public class EqualityConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) => Equals(v, p);
    public object ConvertBack(object v, Type t, object p, CultureInfo c) =>
        (v is bool b && b) ? p! : System.Windows.Data.Binding.DoNothing!;
}
```

#### ThicknessToHeightMulti - Conversion épaisseur/hauteur
```csharp
public class ThicknessToHeightMulti : IMultiValueConverter
{
    public object Convert(object[] values, Type t, object p, CultureInfo c)
    {
        double thick = values.Length > 0 && values[0] is double d ? d : 0;
        double scale = values.Length > 1 && values[1] is double s ? s : 650;
        return Math.Max(2, thick * scale);
    }
}
```

#### RoleToBrushConverter - Couleurs par rôle
```csharp
public class RoleToBrushConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c)
    {
        return v is LayerRole r ? r switch
        {
            LayerRole.Roulement => new SolidColorBrush(Color.FromRgb(11, 92, 142)),
            LayerRole.Base => new SolidColorBrush(Color.FromRgb(217, 90, 78)),
            LayerRole.Fondation => new SolidColorBrush(Color.FromRgb(231, 181, 103)),
            LayerRole.Plateforme => new SolidColorBrush(Color.FromRgb(122, 75, 46)),
            _ => System.Windows.Media.Brushes.LightGray
        } : System.Windows.Media.Brushes.LightGray;
    }
}
```

#### MaterialToBrushConverter - Couleurs par matériau
```csharp
public class MaterialToBrushConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c)
    {
        return v is MaterialFamily mf ? mf switch
        {
            MaterialFamily.BetonBitumineux => System.Windows.Media.Brushes.Black,
            MaterialFamily.GNT => FromHex("#E4B99F"),
            MaterialFamily.MTLH => FromHex("#D3D3D3"),
            MaterialFamily.BetonCiment => FromHex("#A9A9A9"),
            MaterialFamily.Bibliotheque => FromHex("#BEBEBE"),
            _ => System.Windows.Media.Brushes.LightGray
        } : System.Windows.Media.Brushes.LightGray;
    }
}
```

#### EnumDescriptionConverter - Descriptions d'énumérations
```csharp
public class EnumDescriptionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null) return string.Empty;
        var type = value.GetType();
        if (!type.IsEnum) return value.ToString() ?? string.Empty;

        var name = System.Enum.GetName(type, value);
        var field = type.GetField(name!);
        var attr = Attribute.GetCustomAttribute(field!, typeof(System.ComponentModel.DescriptionAttribute))
            as System.ComponentModel.DescriptionAttribute;

        return attr?.Description ?? name ?? string.Empty;
    }
}
```

## Ressources et styles

### Theme.xaml - Thème global

#### Couleurs principales
```xaml
<SolidColorBrush x:Key="PrimaryBrush" Color="#0F2D3A"/>
<SolidColorBrush x:Key="GrayBgBrush" Color="#1F2937"/>
<SolidColorBrush x:Key="CardBg" Color="#FFFFFFFF"/>
<SolidColorBrush x:Key="CardBorder" Color="#E5E7EB"/>
```

#### Styles de boutons
```xaml
<Style x:Key="PrimaryButtonGreen" TargetType="Button">
    <Setter Property="Background" Value="{StaticResource GreenBtn}"/>
    <Setter Property="Foreground" Value="White"/>
    <Setter Property="Padding" Value="8,4"/>
    <Setter Property="BorderThickness" Value="1"/>
    <Setter Property="Cursor" Value="Hand"/>
    <!-- Template personnalisé -->
</Style>
```

#### Effets visuels
```xaml
<DropShadowEffect x:Key="CardShadowEffect"
                  BlurRadius="8"
                  ShadowDepth="1"
                  Opacity="0.18"/>
```

## Flux de communication complet

### 1. Chargement d'un projet
```
1. Utilisateur ouvre fichier → ProjectStorage.Load()
2. Désérialisation JSON → Project avec PavementStructure
3. Mise à jour AppState → CurrentProject et CurrentFilePath
4. ViewModel détecte changement → LoadFromAppState()
5. Synchronisation Layers ← PavementStructure.Layers
6. Mise à jour UI → Data Binding automatique
```

### 2. Modification d'une couche
```
1. Utilisateur modifie propriété → Data Binding
2. ViewModel reçoit changement → Validation domaine
3. Auto-correction si nécessaire → ToastService.ShowToast()
4. Calcul coefficients → OnPropertyChanged(nameof(CoeffKs))
5. Mise à jour affichage → Converter réévalue
6. Synchronisation AppState → UpdateAppState()
```

### 3. Validation complète
```
1. Utilisateur clique "Valider" → ValidateStructureCommand
2. Validation NF P98-086 → ValidateInterfaces() + ValidateGlobalStructure()
3. Collecte erreurs/avertissements → CreateValidationReport()
4. Affichage MessageBox → Style selon sévérité
5. Corrections automatiques → ToastService notifications
```

### 4. Sauvegarde projet
```
1. Utilisateur sauvegarde → ProjectStorage.Save()
2. Synchronisation AppState → UpdateAppState()
3. Sérialisation JSON → Fichier avec indentation
4. Mise à jour RecentFiles → Add(path)
5. Confirmation utilisateur → ToastService.ShowToast()
```

## Patterns et bonnes pratiques

### 1. Séparation des responsabilités
- **Views**: Interface utilisateur et layout uniquement
- **ViewModels**: Logique présentation et commandes
- **Domain**: Règles métier et validation NF P98-086
- **Services**: Fonctionnalités transversales (persistance, notifications)

### 2. Data Binding bidirectionnel
- **OneWay**: Affichage données calculées (CoeffKs, CoeffKd)
- **TwoWay**: Édition propriétés (Thickness_m, Modulus_MPa)
- **OneWayToSource**: Mise à jour source seulement

### 3. Gestion d'état
- **AppState**: État global partagé
- **ObservableCollection**: Notifications changements collections
- **INotifyPropertyChanged**: Notifications changements propriétés

### 4. Validation utilisateur
- **Temps réel**: Validation à chaque modification
- **Auto-correction**: Ajustements automatiques selon norme
- **Feedback visuel**: Indicateurs erreurs dans interface
- **Notifications**: Toasts informatifs pour corrections

### 5. Performance
- **Lazy loading**: Calculs à la demande
- **Virtualisation**: ItemsControl pour grandes listes
- **Caching**: Convertisseurs avec cache si nécessaire
- **Dispatcher**: Opérations UI sur bon thread

## Extension et maintenance

### Ajout d'une nouvelle propriété de couche
1. Ajouter propriété dans `Layer.cs` (domaine)
2. Ajouter validation dans méthodes appropriées
3. Ajouter binding dans `StructureDeChausseeView.xaml`
4. Tester validation et affichage

### Ajout d'un nouveau type de structure
1. Étendre `StructureType` enum dans domaine
2. Ajouter validation dans `ValidateGlobalStructure()`
3. Ajouter dans `StructureTypes` liste ViewModel
4. Tester règles de validation

### Personnalisation de l'interface
1. Modifier styles dans `Theme.xaml`
2. Ajuster couleurs dans `StructureConverters.cs`
3. Personnaliser templates dans vues XAML
4. Tester cohérence visuelle

## Tests et débogage

### Points de test critiques
- **Validation NF P98-086**: Toutes les règles de la norme
- **Data Binding**: Synchronisation View ↔ ViewModel ↔ Domain
- **Persistence**: Sauvegarde/chargement projets
- **Performance**: Grandes structures avec nombreuses couches
- **Ergonomie**: Navigation et feedback utilisateur

### Outils de débogage
- **Output Window**: Logs Data Binding
- **Live Visual Tree**: Inspection hiérarchie visuelle
- **XAML Hot Reload**: Modification interface en cours d'exécution
- **Breakpoints**: Debug logique ViewModels

---

**Document créé le**: 27 août 2025
**Version application**: 1.0
**Norme de référence**: NF P98-086
**Framework**: .NET 8.0 WPF
**Architecture**: MVVM avec séparation claire des responsabilités
