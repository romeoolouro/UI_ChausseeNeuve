# Documentation - Nouveaux Onglets BENIROUTE

## 📊 Onglet Résultats

### Vue d'ensemble
L'onglet "Résultats" affiche les résultats de calcul de la structure de chaussée avec une interface moderne et intuitive.

### Fonctionnalités
- **Validation globale** : Affichage du statut de validation de la structure (valide/non valide)
- **Tableau détaillé** : Résultats par couche avec tous les paramètres calculés
- **Indicateurs visuels** : Code couleur pour la validation de chaque couche
- **Bouton recalculer** : Permet de relancer les calculs

### Données affichées
- Interface (Surface, Fondation, etc.)
- Matériau utilisé
- Niveaux (supérieur/inférieur en cm)
- Module d'élasticité (MPa)
- Coefficient de Poisson
- Contraintes σT (MPa) - valeurs sup/inf
- Déformations εT (μdef) - valeurs sup/inf
- Contrainte verticale σZ (MPa)
- Déformation verticale εZ (μdef)
- Valeur admissible pour comparaison
- Statut de validation (✓/✗)

### Placeholder de calcul
```csharp
// PLACEHOLDER: Implémenter la vraie logique de calcul basée sur:
// - La structure de chaussée définie (AppState.CurrentProject.PavementStructure)
// - Les charges appliquées (AppState.CurrentProject.ChargeReference)
// - Les matériaux sélectionnés dans la bibliothèque

// Algorithme prévu:
// 1. Récupérer les données de structure depuis AppState
// 2. Appliquer les formules de mécanique des chaussées
// 3. Calculer les contraintes et déformations
// 4. Comparer avec les valeurs admissibles
// 5. Déterminer la validité de chaque couche
```

---

## 📋 Onglet Valeurs Admissibles

### Vue d'ensemble
L'onglet "Valeurs Admissibles" permet de configurer les paramètres de calcul et de calculer les valeurs admissibles selon les critères de fatigue.

### Modes de calcul
1. **Calcul manuel** : Paramètres de trafic → Calcul automatique des valeurs admissibles
2. **Saisie directe** : Saisie manuelle des valeurs admissibles

### Paramètres de trafic (Mode manuel)
- **MJA** : Trafic moyen journalier annuel (poids lourds/jour)
- **Taux d'accroissement** : Évolution du trafic en %
- **Type de taux** : Arithmétique ou géométrique
- **Durée de service** : Période de dimensionnement (années)
- **TCPL** : Trafic cumulé calculé automatiquement

### Formules implémentées

#### Calcul du trafic cumulé
```
Arithmétique: TCPL = 365 × MJA × DS × (1 + (DS-1) × TA/200)
Géométrique: TCPL = 365 × MJA × ((1+TA/100)^DS - 1) / (TA/100)
```

#### Valeurs admissibles par critère
```
Déformation horizontale: εt,adm = εt,6 × (N/10^6)^(-1/b) × kc × kr
Contrainte horizontale: σt,adm = σt,6 × (N/10^6)^(-1/b) × kc × kr
Déformation verticale: εz,adm = εz,6 × (N/10^6)^(-1/b) × kc × kr
```

### Paramètres par couche
- **Matériau** : Type de matériau
- **Niveau** : Numéro de couche
- **Critère** : EpsiT, SigmaT, ou EpsiZ
- **Sn** : Valeur de référence à 10^6 cycles
- **Sh** : Valeur de référence alternative
- **b** : Pente de fatigue (négative)
- **kc** : Coefficient de calage
- **kr** : Coefficient de risque
- **Risque** : Niveau de risque de ruine (%)

### Actions disponibles
- ➕ **Ajouter** : Nouvelle couche
- 🗑️ **Supprimer** : Retirer une couche
- 🧮 **Calculer TCPL** : Calcul du trafic cumulé
- 🧮 **Calculer** : Calcul des valeurs admissibles

---

## 🔄 Navigation

### Comment accéder aux nouveaux onglets
1. Lancez l'application BENIROUTE
2. Sélectionnez ou créez un projet
3. Dans la fenêtre principale, utilisez la barre de navigation à gauche
4. Cliquez sur l'icône "Valeurs admissibles" ou "Résultats"
5. La vue correspondante s'affiche dans la zone de contenu

### Workflow recommandé
1. **Structure** : Définir la structure de chaussée
2. **Charges** : Configurer les charges de référence
3. **Bibliothèque** : Sélectionner les matériaux
4. **Valeurs admissibles** : Configurer les paramètres de calcul
5. **Résultats** : Consulter les résultats et validation

---

## 🎨 Design et Interface

### Caractéristiques visuelles
- **Thème sombre moderne** : Cohérent avec le reste de l'application
- **Cartes sectionnées** : Organisation claire du contenu
- **Tableaux interactifs** : Données faciles à lire et modifier
- **Indicateurs visuels** : Code couleur pour les statuts
- **Responsive** : Interface qui s'adapte au contenu

### Codes couleur
- **Vert** : Validation réussie, structure conforme
- **Rouge** : Échec de validation, intervention nécessaire
- **Bleu** : Éléments d'interface et boutons principaux
- **Gris** : Informations secondaires et placeholders

---

## 🔧 Pour les développeurs

### Architecture
- **MVVM** : Separation claire View/ViewModel/Model
- **Commands** : Utilisation de RelayCommand pour les actions
- **Converters** : Conversions de données pour l'affichage
- **Styling** : Styles centralisés dans Theme.xaml

### Fichiers créés
```
ViewModels/
├── ResultatViewModel.cs (logique résultats)
├── ValeursAdmissiblesViewModel.cs (logique valeurs admissibles)
└── RelayCommand.cs (commandes réutilisables)

Views/
├── ResultatView.xaml (interface résultats)
├── ResultatView.xaml.cs
├── ValeursAdmissiblesView.xaml (interface valeurs admissibles)
└── ValeursAdmissiblesView.xaml.cs

Converters/
└── ResultatConverters.cs (conversions spécialisées)
```

### Intégration
- Navigation mise à jour dans `AccueilWindow.xaml.cs`
- Styles ajoutés dans `Resources/Theme.xaml`
- Converters disponibles pour toute l'application

### Prochaines étapes
1. Intégrer avec les services de calcul existants
2. Connecter aux données réelles du domaine
3. Ajouter la persistence des paramètres
4. Implémenter l'export des résultats
5. Tests d'intégration complets
