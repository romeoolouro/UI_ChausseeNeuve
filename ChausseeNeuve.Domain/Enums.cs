using System.ComponentModel;

namespace ChausseeNeuve.Domain.Models;

public enum DimensionnementMode { Expert, Automatique }
public enum StructureType { Souple, BitumineuseEpaisse, SemiRigide, Mixte, Inverse, Rigide }

// Enums pour le syst�me de structure NF P98-086
public enum LayerRole { Roulement = 1, Base = 2, Fondation = 3, Plateforme = 4 }

public enum MaterialFamily
{
    [Description("GNT & Sol")]
    GNT,
    MTLH,
    BetonBitumineux,
    BetonCiment,
    Bibliotheque
}

public enum InterfaceType { Collee, SemiCollee, Decollee }

// Enums pour les charges de r�f�rence
public enum ChargeType
{
    [Description("Jumelage fran�ais")]
    JumelageFrancais,

    [Description("Autre jumelage")]
    AutreJumelage,

    [Description("Roue isol�e")]
    RoueIsolee
}
