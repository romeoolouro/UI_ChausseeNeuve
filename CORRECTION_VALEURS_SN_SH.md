# Correction des valeurs SN et Sh dans la biblioth�que NFP98-086

## Probl�me identifi�
L'utilisateur a signal� des diff�rences entre les valeurs SN et Sh affich�es dans l'application et les vraies valeurs de r�f�rence du catalogue Aliz�/NFP98-086.

## Valeurs corrig�es

### Mat�riaux bitumineux (cat�gorie MB)

| Mat�riau | SN (avant) | SN (corrig�) | Sh (avant) | Sh (corrig�) |
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

## Fichiers modifi�s

### `UI_ChausseeNeuve\Services\MaterialDataService.cs`

1. **M�thode `GetNFP98Defaults()`** : 
   - Correction des valeurs SN pour tous les mat�riaux bitumineux
   - Ajout explicite des valeurs SN et Sh pour les mat�riaux bbm, bbtm, bbdr, acr

2. **M�thode `ApplyNFP98Corrections()`** :
   - Mise � jour de la logique pour respecter les valeurs d�finies dans GetNFP98Defaults()
   - �viter l'�crasement des valeurs d�j� correctement d�finies

## R�gles appliqu�es

### Mat�riaux BBSG, BBME, EME
- **SN = 0.25**
- **Sh = 0.25** 
- **Kc = 1.1** (sauf EME : Kc = 1.0)

### Mat�riaux Graves Bitumes (GB)
- **SN = 0.3**
- **Sh = 0.30**
- **Kc = 1.3**

### Mat�riaux BBM, BBTM, BBDR, ACR
- **SN = 0.25**
- **Sh = 0.25**
- **Kc = 1.1**

## Validation

Pour valider les corrections :

1. Lancer l'application
2. Naviguer vers "Valeurs Admissibles" > "Biblioth�que"
3. S�lectionner "NFP98_086_2019" > "MB"
4. V�rifier que les valeurs SN et Sh correspondent au tableau de r�f�rence fourni

## Script de test

Utiliser le script `test_valeurs_SN_Sh_corrigees.bat` pour lancer un test guid� des corrections.

## Impact

- ? Affichage correct des valeurs SN et Sh dans l'interface utilisateur
- ? Coh�rence avec le tableau de r�f�rence Aliz�/NFP98-086
- ? Calculs des valeurs admissibles bas�s sur les vraies valeurs normatives
- ? Synchronisation parfaite avec les donn�es d'Aliz�

## Notes techniques

- Les valeurs sont directement d�finies dans `GetNFP98Defaults()` pour garantir leur exactitude
- La m�thode `ApplyNFP98Corrections()` respecte les valeurs pr�d�finies et ne les �crase plus
- Support maintenu pour les autres cat�gories (MTLH, B�ton, Sol_GNT) sans r�gression