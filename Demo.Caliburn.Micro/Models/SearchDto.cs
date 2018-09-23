using System;

namespace Demo.Caliburn.Micro.Models
{
    public class SearchDto : IEquatable<SearchDto>
    {
        public string NumberPlate { get; set; }

        public override string ToString()
        {
            return NumberPlate;
        }

        public bool Equals(SearchDto other)
        {
            return string.Equals(ToString(), other?.ToString());
        }

        public override bool Equals(object obj)
        {
            return Equals((SearchDto) obj);
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
    }
}
