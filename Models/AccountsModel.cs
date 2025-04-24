using System.ComponentModel.DataAnnotations;

namespace dotnet_html_sortable_table.Models {
    public class Accounts {

        [Key]
        public int Id {get;set;}

        public int EmpId {get;set;}
        public string NamePrefix {get;set;} = default!;
        public string FirstName { get; set; } = default!;
        public string MiddleInitial { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string Gender { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string FathersName { get; set; } = default!;
        public string MothersName { get; set; } = default!;
        public string MothersMaidenName { get; set; } = default!;
        public string DateOfBirth { get; set; } = default!;
        public string TimeOfBirth { get; set; } = default!;
        public double AgeInYears { get; set; }
        public int WeightInKgs { get; set; }
        public string DateOfJoining { get; set; } = default!;
        public string QuarterOfJoining { get; set; } = default!;
        public string HalfOfJoining { get; set; } = default!;
        public int YearOfJoining { get; set; }
        public int MonthOfJoining { get; set; }
        public string MonthNameOfJoining { get; set; } = default!;
        public string ShortMonth { get; set; } = default!;
        public int DayOfJoining { get; set; }
        public string DOWOfJoining { get; set; } = default!;
        public string ShortDOW { get; set; } = default!;
        public double AgeInCompanyYears { get; set; }
        public int Salary { get; set; }
        public string LastPercentHike { get; set; } = default!;
        public string SSN { get; set; } = default!;
        public string PhoneNumber { get; set; } = default!;
        public string PlaceName { get; set; } = default!;
        public string County { get; set; } = default!;
        public string City { get; set; } = default!;
        public string State { get; set; } = default!;
        public string Zip { get; set; } = default!;
        public int Region { get; set; }
        public string UserName { get; set; } = default!;
        public string Password { get; set; } = default!;
    }
}
