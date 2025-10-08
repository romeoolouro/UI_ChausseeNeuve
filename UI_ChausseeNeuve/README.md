# UI_ChausseeNeuve (Interface WPF)

Interface utilisateur principale de l'application de dimensionnement de chaussées, développée en WPF avec .NET 8.

## Architecture

### Workflow en 3 Étapes
1. **Sélection du mode** - Expert ou Automatique
2. **Informations projet** - Titre, auteur, emplacement, description
3. **Interface principale** - Calculs et résultats

### Organisation du Code

#### ViewModels/
- ViewModels MVVM pour chaque fenêtre et composant
- Logique de présentation et binding des données

#### Views/
- Vues XAML pour l'interface utilisateur
- Contrôles utilisateur et pages

#### Windows/
- Fenêtres principales de l'application
- Dialogs et fenêtres modales

#### Services/
- Services applicatifs (calculs, persistance, etc.)
- Intégration avec le moteur de calcul

#### Converters/
- Convertisseurs XAML pour le binding
- Formatage et transformation des données

#### Resources/
- Ressources partagées (styles, templates, images)
- Dictionnaires de ressources XAML

## Configuration

### AppState.cs
Gestionnaire d'état global partagé entre les fenêtres.

### App.xaml/App.xaml.cs
Configuration de l'application et démarrage.

## Lancement

```bash
dotnet run --project UI_ChausseeNeuve
```