using System;
using ChausseeNeuve.Domain.Models;

namespace UI_ChausseeNeuve.Services
{
    /// <summary>
    /// Service de mapping entre les catégories de matériaux legacy et le nouveau système MaterialFamily
    /// </summary>
    public static class MaterialMappingService
    {
        /// <summary>
        /// Mappe une catégorie legacy vers un MaterialFamily
        /// </summary>
        /// <param name="legacyCategory">Catégorie legacy (MB, MTLH, Beton, Sol_GNT)</param>
        /// <param name="libraryName">Nom de la bibliothèque pour contexte</param>
        /// <returns>MaterialFamily correspondant</returns>
        public static MaterialFamily MapLegacyCategoryToMaterialFamily(string legacyCategory, string libraryName)
        {
            return (legacyCategory, libraryName) switch
            {
                // MB (Matériaux Bitumineux) → Bibliotheque avec sous-type MB
                ("MB", _) => MaterialFamily.Bibliotheque,

                // MTLH → MTLH
                ("MTLH", _) => MaterialFamily.MTLH,

                // Beton → BetonCiment ou BetonBitumineux selon le contexte
                ("Beton", "MateriauxBenin") => MaterialFamily.BetonCiment,
                ("Beton", "CatalogueSenegalais") => MaterialFamily.BetonCiment,
                ("Beton", "CatalogueFrancais1998") => MaterialFamily.BetonCiment,
                ("Beton", "NFP98_086_2019") => MaterialFamily.BetonCiment,
                ("Beton", "MateriauxUser") => MaterialFamily.BetonCiment,
                ("Beton", _) => MaterialFamily.BetonCiment,

                // Sol_GNT → GNT
                ("Sol_GNT", _) => MaterialFamily.GNT,

                // Par défaut
                _ => MaterialFamily.Bibliotheque
            };
        }

        /// <summary>
        /// Obtient le nom d'affichage pour une catégorie legacy
        /// </summary>
        /// <param name="legacyCategory">Catégorie legacy</param>
        /// <returns>Nom d'affichage</returns>
        public static string GetLegacyCategoryDisplayName(string legacyCategory)
        {
            return legacyCategory switch
            {
                "MB" => "Matériaux Bitumineux",
                "MTLH" => "MTLH",
                "Beton" => "Béton",
                "Sol_GNT" => "Sol et GNT",
                _ => legacyCategory
            };
        }

        /// <summary>
        /// Obtient le nom d'affichage pour une bibliothèque
        /// </summary>
        /// <param name="libraryName">Nom de la bibliothèque</param>
        /// <returns>Nom d'affichage</returns>
        public static string GetLibraryDisplayName(string libraryName)
        {
            return libraryName switch
            {
                "MateriauxBenin" => "Matériaux du Bénin",
                "CatalogueSenegalais" => "Catalogue Sénégalais",
                "CatalogueFrancais1998" => "Catalogue Français 1998",
                "NFP98_086_2019" => "NF P 98-086 2019",
                "MateriauxUser" => "Matériaux Utilisateur",
                _ => libraryName
            };
        }

        /// <summary>
        /// Valide qu'une combinaison bibliothèque/catégorie est supportée
        /// </summary>
        /// <param name="libraryName">Nom de la bibliothèque</param>
        /// <param name="legacyCategory">Catégorie legacy</param>
        /// <returns>True si la combinaison est valide</returns>
        public static bool IsValidCombination(string libraryName, string legacyCategory)
        {
            // Toutes les bibliothèques supportent toutes les catégories
            string[] validLibraries = { "MateriauxBenin", "CatalogueSenegalais", "CatalogueFrancais1998", "NFP98_086_2019", "MateriauxUser" };
            string[] validCategories = { "MB", "MTLH", "Beton", "Sol_GNT" };

            return Array.Exists(validLibraries, lib => lib == libraryName) &&
                   Array.Exists(validCategories, cat => cat == legacyCategory);
        }

        /// <summary>
        /// Obtient les propriétés par défaut pour un type de matériau
        /// </summary>
        /// <param name="materialFamily">Famille de matériau</param>
        /// <returns>Tuple (ModuleYoung, CoefficientPoisson)</returns>
        public static (double modulus, double poisson) GetDefaultMaterialProperties(MaterialFamily materialFamily)
        {
            return materialFamily switch
            {
                MaterialFamily.GNT => (50.0, 0.35),      // Sol et GNT
                MaterialFamily.MTLH => (10000.0, 0.25),  // MTLH
                MaterialFamily.BetonCiment => (35000.0, 0.20),  // Béton de ciment
                MaterialFamily.BetonBitumineux => (8000.0, 0.30), // Béton bitumineux
                MaterialFamily.Bibliotheque => (5000.0, 0.35),   // Bibliothèque (MB)
                _ => (1000.0, 0.35)  // Valeurs par défaut conservatrices
            };
        }
    }
}
