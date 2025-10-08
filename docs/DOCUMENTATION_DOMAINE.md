# Documentation Technique - Domaine ChausseeNeuve.Domain

## Vue d'ensemble

Le domaine `ChausseeNeuve.Domain` constitue le cœur métier de l'application ChausseeNeuve, spécialisé dans la conception et la validation de structures de chaussée selon la norme française NF P98-086.

## Architecture

### Structure du projet
```
ChausseeNeuve.Domain/
├── ChausseeNeuve.Domain.csproj    # Configuration .NET 8.0
├── Enums.cs                       # Énumérations métier
├── Layer.cs                       # Modèle de couche avec validation NF P98-086
├── PavementStructure.cs           # Structure complète de chaussée
└── Project.cs                     # Projet principal intégrant la structure
```

### Technologies utilisées
- **Framework**: .NET 8.0
- **Paradigme**: Programmation orientée objet avec validation de données
- **Interfaces**: INotifyPropertyChanged, INotifyDataErrorInfo
- **Norme**: NF P98-086 (Dimensionnement des structures de chaussée)

## Classes et responsabilités

### 1. Enums.cs - Énumérations métier

#### DimensionnementMode
```csharp
public enum DimensionnementMode { Expert, Automatique }
```
- **Expert**: Mode manuel avec contrôle total des paramètres
- **Automatique**: Mode assisté avec suggestions de valeurs

#### StructureType
```csharp
public enum StructureType {
    Souple, BitumineuseEpaisse, SemiRigide, Mixte, Inverse, Rigide
}
```
Types de structures de chaussée selon la classification NF P98-086.

#### LayerRole
```csharp
public enum LayerRole { Roulement = 1, Base = 2, Fondation = 3, Plateforme = 4 }
```
Rôles hiérarchiques des couches dans la structure :
- **Roulement**: Couche de surface (usure)
- **Base**: Couche de base (portance)
- **Fondation**: Couche de fondation (transition)
- **Plateforme**: Plate-forme support (infrastructure)

#### MaterialFamily
```csharp
public enum MaterialFamily
{
    [Description("GNT & Sol")] GNT,
    MTLH,
    BetonBitumineux,
    BetonCiment,
    Bibliotheque
}
```
Familles de matériaux selon NF P98-086 :
- **GNT**: Graves Non Traitées
- **MTLH**: Mélanges Traités aux Liants Hydrauliques
- **BetonBitumineux**: Enrobés bitumineux
- **BetonCiment**: Bétons de ciment
- **Bibliotheque**: Matériaux personnalisés

#### InterfaceType
```csharp
public enum InterfaceType { Collee, SemiCollee, Decollee }
```
Types d'interfaces entre couches selon la norme.

### 2. Layer.cs - Modèle de couche avec validation

#### Responsabilités principales
- **Validation automatique** des propriétés selon NF P98-086
- **Calcul des coefficients** Ks/Kd
- **Gestion des erreurs** avec INotifyDataErrorInfo
- **Notifications utilisateur** via système toast

#### Propriétés principales

##### Propriétés de base
```csharp
public int Order { get; set; }              // Position dans la structure (1 = surface)
public LayerRole Role { get; set; }         // Rôle de la couche
public string MaterialName { get; set; }    // Nom commercial du matériau
public MaterialFamily Family { get; set; }  // Famille de matériau
```

##### Propriétés mécaniques (avec validation automatique)
```csharp
public double Thickness_m { get; set; }      // Épaisseur en mètres (validée)
public double Modulus_MPa { get; set; }     // Module d'élasticité (validé)
public double Poisson { get; set; }         // Coefficient de Poisson (validé)
```

##### Coefficients NF P98-086
```csharp
public double CoeffKs { get; }              // Coefficient de structure (calculé)
public double CoeffKd { get; }              // Coefficient de déformation (calculé)
```

##### Interface
```csharp
public InterfaceType? InterfaceWithBelow { get; set; }  // Type d'interface avec couche inférieure
```

#### Validation automatique

##### Validation d'épaisseur
- **GNT**: 0.10m - 0.35m
- **MTLH**: 0.15m - 0.32m
- **Béton Bitumineux**: 0.05m - 0.16m
- **Béton Ciment**: 0.12m - 0.45m
- **Bibliothèque**: 0.01m - 2.0m (plage large)

##### Validation de module
- **GNT**: 100 - 1000 MPa
- **MTLH**: 3000 - 32000 MPa
- **Béton Bitumineux**: 3000 - 18000 MPa
- **Béton Ciment**: 18000 - 40000 MPa

##### Validation de Poisson
- **GNT/Béton Bitumineux**: 0.35 (fixé)
- **MTLH/Béton Ciment**: 0.25 (fixé)

#### Calcul des coefficients

##### Coefficient Ks (Structure)
Calculé selon Section 6.2.2 NF P98-086 :
- Varie selon le rôle de la couche et la famille de matériau
- Exemple: Roulement en béton bitumineux = 1.0

##### Coefficient Kd (Déformation)
Calculé selon Section 6.2.3 NF P98-086 :
- Base selon matériau + ajustement selon épaisseur
- Exemple: GNT = 2.0 × multiplicateur d'épaisseur

#### Système de notifications
```csharp
public static Action<string, ToastType>? NotifyToast { get; set; }
```
Permet l'injection d'un système de notifications pour informer l'utilisateur des corrections automatiques.

### 3. PavementStructure.cs - Structure complète

#### Responsabilités
- Conteneur pour l'ensemble des couches
- Métadonnées de la structure
- Gestion de la liste ordonnée des couches

#### Propriétés
```csharp
public List<Layer> Layers { get; }           // Liste ordonnée des couches (index 0 = surface)
public double NE { get; set; }               // Nombre équivalent d'essieux (défaut: 80,000)
public string StructureType { get; set; }    // Type de structure (défaut: "Souple")
```

### 4. Project.cs - Projet principal

#### Responsabilités
- Conteneur principal du projet
- Intégration de la structure de chaussée
- Métadonnées générales du projet

#### Propriétés
```csharp
public string Name { get; set; }             // Nom du projet
public string Author { get; set; }           // Auteur du projet
public string Location { get; set; }         // Localisation
public string Description { get; set; }      // Description détaillée
public DateTime CreatedAt { get; set; }      // Date de création
public DimensionnementMode Mode { get; set; } // Mode de dimensionnement
public PavementStructure PavementStructure { get; set; } // Structure de chaussée
```

## Relations entre classes

```
Project
├── PavementStructure
    ├── Layers (List<Layer>)
    ├── NE
    └── StructureType
```

Chaque `Layer` contient :
- Référence à sa `MaterialFamily`
- Calcul automatique des coefficients Ks/Kd
- Validation selon NF P98-086
- Gestion d'erreurs avec INotifyDataErrorInfo

## Utilisation typique

### Création d'une nouvelle couche
```csharp
var layer = new Layer
{
    Order = 1,
    Role = LayerRole.Roulement,
    MaterialName = "BBTM 0/10",
    Family = MaterialFamily.BetonBitumineux,
    Thickness_m = 0.08,      // Sera validé automatiquement
    Modulus_MPa = 12000,     // Sera validé automatiquement
    Poisson = 0.35           // Sera corrigé automatiquement
};

// Les coefficients sont calculés automatiquement
double ks = layer.CoeffKs;  // 1.0 pour roulement bitumineux
double kd = layer.CoeffKd;  // Calculé selon épaisseur
```

### Création d'une structure complète
```csharp
var structure = new PavementStructure
{
    NE = 100000,
    StructureType = "Souple"
};

// Ajout des couches (ordre: surface vers profondeur)
structure.Layers.Add(new Layer { /* couche de roulement */ });
structure.Layers.Add(new Layer { /* couche de base */ });
structure.Layers.Add(new Layer { /* couche de fondation */ });
```

### Intégration dans un projet
```csharp
var project = new Project
{
    Name = "Autoroute A1 - Section 42",
    Author = "Ingénieur Jean Dupont",
    Location = "Nord-Pas-de-Calais",
    Mode = DimensionnementMode.Expert,
    PavementStructure = structure
};
```

## Conventions de développement

### 1. Validation automatique
Toutes les propriétés mécaniques sont automatiquement validées selon NF P98-086 lors de leur modification.

### 2. Notifications
Le système utilise un pattern d'injection de dépendance pour les notifications toast.

### 3. Calculs en temps réel
Les coefficients Ks/Kd sont recalculés automatiquement à chaque changement affectant leur valeur.

### 4. Gestion d'erreurs
Utilisation de INotifyDataErrorInfo pour exposer les erreurs de validation à l'interface utilisateur.

## Extension et maintenance

### Ajout d'une nouvelle famille de matériau
1. Ajouter la valeur dans `MaterialFamily`
2. Implémenter les plages de validation dans `Layer.cs`
3. Définir les coefficients Ks/Kd dans les méthodes de calcul
4. Ajouter la couleur correspondante dans l'interface utilisateur

### Modification des règles de validation
Les méthodes de validation sont centralisées dans `Layer.cs` :
- `ValidateThickness()` : Règles d'épaisseur
- `ValidateModulus()` : Règles de module
- `ValidatePoisson()` : Règles de coefficient de Poisson

### Tests
Le domaine est conçu pour être facilement testable grâce à :
- Injection de dépendance pour les notifications
- Méthodes de validation isolées
- Propriétés calculées pures

## Conformité NF P98-086

### Sections implémentées
- **Section 6.2.2**: Coefficient de structure Ks
- **Section 6.2.3**: Coefficient de déformation Kd
- **Section 6.2.3.2**: Ajustement Kd selon épaisseur
- **Annexes**: Plages de valeurs par matériau

### Validations automatiques
- Épaisseurs selon familles de matériaux
- Modules d'élasticité selon familles
- Coefficients de Poisson normalisés
- Correction intelligente des valeurs hors normes

## Migration et compatibilité

### Depuis versions antérieures
Le domaine est conçu pour être rétrocompatible. Les projets existants peuvent être migrés en :
1. Créant une `PavementStructure` vide
2. Convertissant les anciennes données de couche
3. Appliquant les validations NF P98-086

### Évolutivité
L'architecture permet facilement :
- Ajout de nouvelles propriétés de couche
- Extension des règles de validation
- Intégration de nouvelles normes
- Personnalisation par matériau

---

**Document créé le**: 27 août 2025
**Version domaine**: 1.0
**Norme de référence**: NF P98-086
**Framework**: .NET 8.0</content>
<parameter name="filePath">d:\Codes\C#\BENI\ChausseeNeuve_3Windows\DOCUMENTATION_DOMAINE.md
