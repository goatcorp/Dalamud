namespace Dalamud.Bindings.ImPlot
{
    using System;

    public struct Tm : IEquatable<Tm>
    {
        /// <summary>
        /// seconds after the minute - [0, 60] including leap second
        /// </summary>
        public int Sec;

        /// <summary>
        /// minutes after the hour - [0, 59]
        /// </summary>
        public int Min;

        /// <summary>
        /// hours since midnight - [0, 23]
        /// </summary>
        public int Hour;

        /// <summary>
        /// day of the month - [1, 31]
        /// </summary>
        public int MDay;

        /// <summary>
        /// months since January - [0, 11]
        /// </summary>
        public int Mon;

        /// <summary>
        /// years since 1900
        /// </summary>
        public int Year;

        /// <summary>
        /// days since Sunday - [0, 6]
        /// </summary>
        public int WDay;

        /// <summary>
        /// days since January 1 - [0, 365]
        /// </summary>
        public int YDay;

        /// <summary>
        /// daylight savings time flag
        /// </summary>
        public int IsDst;

        public override bool Equals(object? obj)
        {
            return obj is Tm tm && Equals(tm);
        }

        public bool Equals(Tm other)
        {
            return Sec == other.Sec &&
                   Min == other.Min &&
                   Hour == other.Hour &&
                   MDay == other.MDay &&
                   Mon == other.Mon &&
                   Year == other.Year &&
                   WDay == other.WDay &&
                   YDay == other.YDay &&
                   IsDst == other.IsDst;
        }

        public override int GetHashCode()
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
            HashCode hash = new HashCode();
            hash.Add(Sec);
            hash.Add(Min);
            hash.Add(Hour);
            hash.Add(MDay);
            hash.Add(Mon);
            hash.Add(Year);
            hash.Add(WDay);
            hash.Add(YDay);
            hash.Add(IsDst);
            return hash.ToHashCode();
#else
            int hash = 17;
            hash = hash * 31 + Sec.GetHashCode();
            hash = hash * 31 + Min.GetHashCode();
            hash = hash * 31 + Hour.GetHashCode();
            hash = hash * 31 + MDay.GetHashCode();
            hash = hash * 31 + Mon.GetHashCode();
            hash = hash * 31 + Year.GetHashCode();
            hash = hash * 31 + WDay.GetHashCode();
            hash = hash * 31 + YDay.GetHashCode();
            hash = hash * 31 + IsDst.GetHashCode();
            return hash;
#endif
        }

        public static bool operator ==(Tm left, Tm right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Tm left, Tm right)
        {
            return !(left == right);
        }
    }
}
