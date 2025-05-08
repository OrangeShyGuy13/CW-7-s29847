namespace DefaultNamespace;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using CW-7-s29847.Models;
using CW-7-s29847.Models.DTOs;
[ApiController]
[Route("api/[controller]")]
public class TripsController : ControllerBase
{
    private readonly string _url;

    public TripsController(IConfiguration configuration)
    {
        _url = configuration.GetConnectionString("DatabaseConnection")
    }
    [HttpGet]
    public async Task<IActionResult> GetTrips()
    {
        var trips = new List<TripGetCountryDTO>();
        using var conn = new SqlConnection(url);
        await conn.OpenAsync();
        // select finding all trips and associated countries
        var cmd = new SqlCommand(
            "SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, Country.name  from Trip t join Country_Trip ON t.IdTrip = Country_Trip.IdTrip join Country on Country_Trip.IdCountry = Country.IdCountry",
            conn);
        using var reader = await cmd.ExecuteReaderAsync();
        try
        {
            while (await reader.ReadAsync())
            {
                trips.Add(new TripGetCountryDTO
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.IsDbNull(2) ? null : reader.GetString(2),
                    DateFrom = reader.GetDateTime(3),
                    DateTo = reader.GetDateTime(4),
                    MaxPeople = reader.GetInt32(5),
                    CountryName = reader.GetString(6)
                });
            }

            return Ok(trips);
        }
        catch (Exception e)
        {
            return StatusCode(500,"Server error");
        }
    }

    [HttpGet("{id}/trips")]
    public async Task<IActionResult> GetClientTrips(int id)
    {
        //end point for getting all client registred trips with their registration and payment date if paid
        var trips = new List<ClientGetTripsDTO>();
        using var conn = new SqlConnection(url);
        await conn.OpenAsync();
        //verify if client exists
        var cmdClientCheck = new SqlCommand("Select * from Client where IdClient = @id", conn);
        cmdClientCheck.Parameters.AddWithValue("@id", id);
        var exists = await cmdClientCheck.ExecuteScalarAsync();
        if (exists == null)
        {
            return NotFound($"Klient {id} nie istnieje");
        }

        // sql query connecting client with client trip and trips returning trip data with client_trip registration data and payment
        var cmd = new SqlCommand(
            "SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, ct.RegisteredAt, ct.PaymentDate from Client c join Client_Trip ct on c.IdClient = ct.IdClient join Trip t on ct.IdTrip = t.IdTrip where c.IdClient = @id",
            conn);
        using var reader = await cmd.ExecuteReaderAsync();
        try
        {
            while (await reader.ReadAsync())
            {
                trips.Add(new ClientGetTripsDTO
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.IsDbNull(2) ? null : reader.GetString(2),
                    DateFrom = reader.GetDateTime(3),
                    DateTo = reader.GetDateTime(4),
                    MaxPeople = reader.GetInt32(5),
                    RegisteredAt = reader.GetInt32(6),
                    PaymentDate = reader.IsDbNull(7) ? null : reader.GetInt32(7)
                })
            }
        }
        catch(Exception e)
        {
            return StatusCode(500,"Server error");
        }
        // jeśli znaleziono 0 wycieczek dla klienta
        if (trips.Count == 0)
        {
            return NotFound($"Klient {id} nie ma wycieczek");
        }
        return Ok(trips)
    }

    [HttpPost]
    public async Task<IActionResult> CreateClient([FromBody] ClientCreateDBO client)
    {
        //check if model is correct
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        using var conn = new SqlConnection(url);
        await conn.OpenAsync();
        //data insertions
        var cmd = new SqlCommand("INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel) OUTPUT INSERTED.IdCLient VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel)", conn);
        cmd.Parameters.AddWithValue("@FirstName", client.FirstName);
        cmd.Parameters.AddWithValue("@LastName", client.LastName);
        cmd.Parameters.AddWithValue("@Email", client.Email);
        cmd.Parameters.AddWithValue("@Telephone", client.Telephone ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Pesel", string.IsNullOrEmpty(client.Pesel) ? DBNull.Value : client.Pesel);
        //return id
        int insertedClient = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        return Created($"Utworzono klienta {insertedClient}");
    }

    [HttpPut("/clients/{id}/trips/{tripId}")]
    public async Task<IActionResult> RegisterClient(int id, int tripId)
    {
        //check if client and trip exist
        using var conn = new SqlConnection(url);
        await conn.OpenAsync();
        var cmdClientExist = new SqlCommand("SELECT Id FROM Client where IdClient = @id", conn);
        cmdClientExist.Parameters.AddWithValue("@id", id);
        var cexists = await cmdClientExist.ExecuteScalarAsync();
        if (cexists == null)
        {
            return NotFound($"Klient {id} nie istnieje");
        }
        var cmdTripCheck = new SqlCommand("Select * from Trip where IdTrip = @tripId", conn);
        cmdTripCheck.Parameters.AddWithValue("@tripId", tripId);
        var texists = await cmdTripCheck.ExecuteScalarAsync();
        if (texists == null)
        {
            return NotFound($"Wycieczka {tripId} nie istnieje");
        }
        //check how many people are currently enrolled
        var cmdEnrolledClients =
            new SqlCommand(
                "SELECT c.IdClient, c.FirstName, c.LastName, c.Email, c.Telephone, c.Pesel FROM Trip t join Client_Trip ct on t.IdTrip = ct.IdTrip join Client c on ct.IdClient = c.IdClient",
                conn);
        var enrolledClients = new List<Client>();
        while (await reader.ReadAsync())
        {
            enrolledClients.Add(new Client
            {
                IdClient = reader.GetInt32(0),
                FirstName = reader.GetString(1),
                LastName = reader.GetString(2),
                Email = reader.GetString(3),
                Telephone = reader.isDBNull(4) ? null : reader.GetString(4)
                Pesel = reader.isDBNull(5) ? null : reader.GetString(5)
            });
        }
        //check max number of enrolled people
        var cmdMaxCheck = new SqlCommand("Select MaxPeople from Trip where IdTrip = @tripId", conn);
        cmdMaxCheck.Parameters.AddWithValue("@tripId", tripId);
        int max = Convert.ToInt32(await cmdMaxCheck.executeScalarAsync());
        if (enrolledClients.Count == max)
        {
            return BadRequest($"Wycieczka {tripId} jest juz zapelniona");
        }
        //insert client data into database
        var cmdAddClientTrip = new SqlCommand("INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt, PaymentDate) values (@IdClient, @IdTrip, @RegisteredAt, NULL)", conn);
        cmdAddClientTrip.AddWithValue("@IdClient", id);
        cmdAddClientTrip.AddWithValue("@IdTrip", tripId);
        cmdAddClientTrip.AddWithValue("@RegisteredAt", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        try
        {
            await cmdAddClientTrip.ExecuteNonQueryAsync();
            return OK;
        }
        catch (Exception e){
            return StatusCode(500,"Server error");
        }
    }

    [HttpDelete("/clients/{id}/trips/{tripId}")]
    public async Task<IActionResult> DeleteRegistration(int id, int tripId)
    {
        //check if client and trip exist
        using var conn = new SqlConnection(url);
        await conn.OpenAsync();
        //select row to check if client exists
        var cmdClientExist = new SQLCommand("SELECT Id FROM Client where IdClient = @id", conn);
        cmdClientExist.Parameters.AddWithValue("@id", id);
        var cexists = await cmdClientExist.ExecuteScalarAsync();
        if (cexists == null)
        {
            return NotFound($"Klient {id} nie istnieje")
        }
        // select row to check if trip exists in trips
        var cmdTripCheck = new SqlCommand("Select * from Trip where IdTrip = @tripId", conn);
        cmdClientCheck.Parameters.AddWithValue("@tripId", tripId);
        var texists = await cmdClientCheck.ExecuteScalarAsync();
        if (texists == null)
        {
            return NotFound($"Wycieczka {tripId} nie istnieje")
        }
        // select row from client_trip to check if it exists
        var cmdRegistrationExists = new SqlCommand("Select * from Client_Trip where IdClient = @id and IdTrip = @tripId", conn);
        cmdRegistrationExists.Parameters.AddWithValue("@id", id);
        cmdRegistrationExists.Parameters.AddWithValue("@tripId", tripId);
        var rexists = await cmdRegistrationExists.ExecuteScalarAsync();
        if (rexists == null)
        {
            return NotFound($"Klient {id} nie jest zapisany na wycieczke {tripId}");
        }
        // sql query to delete row from client_trip where id and trip id match request 
        var cmdDeleteRegistration = new SqlCommand("Delete from Client_Trip where IdTrip = @tripId and IdClient = @id", conn);
        cmdDeleteRegistration.Parameters.AddWithValue("@id", id);
        cmdDeleteRegistration.Parameters.AddWithValue("@tripId", tripId);
        int rowsAffected = Convert.ToInt32(await cmdDeleteRegistration.ExecuteNonQueryAsync());
        if (rowsAffected > 0)
        {
            return Ok($"Usunieto rejestracje {id} na wycieczke {tripId}");
        }
        else
        {
            return NotFound("Nie znaleziono rejestracji");
        }
    }
}