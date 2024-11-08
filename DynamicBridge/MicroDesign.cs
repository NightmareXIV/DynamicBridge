using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge
{
    public class MicroDesign : IEquatable<MicroDesign>
    {
        public string WholeData = "";
        public string Name = "";
        public string Identifier = Guid.NewGuid().ToString();

        public override bool Equals(object obj)
        {
            return Equals(obj as MicroDesign);
        }

        public bool Equals(MicroDesign other)
        {
            return other is not null &&
                   Identifier == other.Identifier;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Identifier);
        }

        public static bool operator ==(MicroDesign left, MicroDesign right)
        {
            return EqualityComparer<MicroDesign>.Default.Equals(left, right);
        }

        public static bool operator !=(MicroDesign left, MicroDesign right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return $"MicroDesign(name={Name}, id={Identifier})";
        }
    }
}
