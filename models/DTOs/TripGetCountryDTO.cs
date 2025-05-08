namespace CW-7-s29847.Models.DTOs;

public class TripGetCountryDTO
{
    public int IdTrip { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public int MaxPeople { get; set; }
    public string CountryName { get; set; }
}