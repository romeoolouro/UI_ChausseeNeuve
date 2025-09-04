using System.Collections.Generic;

namespace ChausseeNeuve.Domain.Models
{
    public class PavementStructure
    {
        public List<Layer> Layers { get; } = new();
        public double NE { get; set; } = 80_000;
        public string StructureType { get; set; } = "Souple";

        // Charges de référence
        public ChargeReference ChargeReference { get; set; } = new ChargeReference();
    }
}
