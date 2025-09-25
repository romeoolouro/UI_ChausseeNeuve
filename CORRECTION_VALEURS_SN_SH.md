# Correction des valeurs SN et Sh dans la bibliothèque NFP98-086

## Problème identifié
L'utilisateur a signalé des différences entre les valeurs SN et Sh affichées dans l'application et les vraies valeurs de référence du catalogue Alizé/NFP98-086.

## Valeurs corrigées

### Matériaux bitumineux (catégorie MB)

| Matériau | SN (avant) | SN (corrigé) | Sh (avant) | Sh (corrigé) |
|----------|------------|--------------|------------|--------------|
| eb-bbsg1 | 5 | 0.25 | 0.25 | 0.25 |
| eb-bbsg2 | 5 | 0.25 | 0.25 | 0.25 |
| eb-bbsg3 | 5 | 0.25 | 0.25 | 0.25 |
| eb-bbme1 | 5 | 0.25 | 0.25 | 0.25 |
| eb-bbme2 | 5 | 0.25 | 0.25 | 0.25 |
| eb-bbme3 | 5 | 0.25 | 0.25 | 0.25 |
| bbm | - | 0.25 | 0.25 | 0.25 |
| bbtm | - | 0.25 | 0.25 | 0.25 |
| bbdr | - | 0.25 | 0.25 | 0.25 |
| acr | - | 0.25 | 0.25 | 0.25 |
| eb-gb2 | 5 | 0.3 | 0.30 | 0.30 |
| eb-gb3 | 5 | 0.3 | 0.30 | 0.30 |
| eb-gb4 | 5 | 0.3 | 0.30 | 0.30 |
| eb-eme1 | 5 | 0.25 | 0.25 | 0.25 |
| eb-eme2 | 5 | 0.25 | 0.25 | 0.25 |

## Fichiers modifiés

### `UI_ChausseeNeuve\Services\MaterialDataService.cs`

1. **Méthode `GetNFP98Defaults()`** : 
   - Correction des valeurs SN pour tous les matériaux bitumineux
   - Ajout explicite des valeurs SN et Sh pour les matériaux bbm, bbtm, bbdr, acr

2. **Méthode `ApplyNFP98Corrections()`** :
   - Mise à jour de la logique pour respecter les valeurs définies dans GetNFP98Defaults()
   - Éviter l'écrasement des valeurs déjà correctement définies

## Règles appliquées

### Matériaux BBSG, BBME, EME
- **SN = 0.25**
- **Sh = 0.25** 
- **Kc = 1.1** (sauf EME : Kc = 1.0)

### Matériaux Graves Bitumes (GB)
- **SN = 0.3**
- **Sh = 0.30**
- **Kc = 1.3**

### Matériaux BBM, BBTM, BBDR, ACR
- **SN = 0.25**
- **Sh = 0.25**
- **Kc = 1.1**

## Validation

Pour valider les corrections :

1. Lancer l'application
2. Naviguer vers "Valeurs Admissibles" > "Bibliothèque"
3. Sélectionner "NFP98_086_2019" > "MB"
4. Vérifier que les valeurs SN et Sh correspondent au tableau de référence fourni

## Script de test

Utiliser le script `test_valeurs_SN_Sh_corrigees.bat` pour lancer un test guidé des corrections.

## Impact

- ? Affichage correct des valeurs SN et Sh dans l'interface utilisateur
- ? Cohérence avec le tableau de référence Alizé/NFP98-086
- ? Calculs des valeurs admissibles basés sur les vraies valeurs normatives
- ? Synchronisation parfaite avec les données d'Alizé

## Notes techniques

- Les valeurs sont directement définies dans `GetNFP98Defaults()` pour garantir leur exactitude
- La méthode `ApplyNFP98Corrections()` respecte les valeurs prédéfinies et ne les écrase plus
- Support maintenu pour les autres catégories (MTLH, Béton, Sol_GNT) sans régression