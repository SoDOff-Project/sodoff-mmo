using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using sodoffmmo.Core;
using sodoffmmo.Data;

namespace sodoffmmo.API.Controllers
{
    [Route("mmo/update")]
    [ApiController]
    public class UserUpdateController : ControllerBase
    {
        [HttpPost]
        [Route("SendPacketToPlayer")]
        public IActionResult SendPacketToPlayer([FromForm] string apiToken, [FromForm] string userId, [FromForm] string cmd, [FromForm] string serializedArgs)
        {
            Client? client = Server.AllClients.FirstOrDefault(e => e.PlayerData.UNToken == apiToken);
            Client? receivingClient = Server.AllClients.FirstOrDefault(e => e.PlayerData.Uid == userId);

            if (client == null) return Unauthorized();

            // send string array packet to player
            string[]? deserializedArgs = JsonSerializer.Deserialize<string[]>(serializedArgs);
            if(deserializedArgs == null) return BadRequest("No Data");
            if(receivingClient != null) receivingClient.Send(Utils.ArrNetworkPacket(deserializedArgs, cmd));
            else return BadRequest("Client Not Found");

            return Ok();
        }

        [HttpPost]
        [Route("SendPacketToRoom")]
        public IActionResult SendPacketToRoom([FromForm] string apiToken, [FromForm] string roomName, [FromForm] string cmd, [FromForm] string serializedArgs)
        {
            Client? client = Server.AllClients.FirstOrDefault(e => e.PlayerData.UNToken == apiToken);
            Room? room = Room.Get(roomName);

            if(client == null) return Unauthorized();
            else if (room == null) return BadRequest("Room Not Found");

            // send string array packet to room
            string[]? deserializedArgs = JsonSerializer.Deserialize<string[]>(serializedArgs);
            if(deserializedArgs == null) return BadRequest("No Data");
            room.Send(Utils.ArrNetworkPacket(deserializedArgs, cmd));

            return Ok();
        }

        [HttpPost]
        [Route("UpdateRoomVarsInRoom")]
        public IActionResult UpdateRoomVarsInRoom([FromForm] string apiToken, [FromForm] string roomName, [FromForm] string serializedVars)
        {
            // vars are stored as key value pairs in dictionaries, grab a dictionary instead of a string array
            Dictionary<string, string>? vars = JsonSerializer.Deserialize<Dictionary<string, string>>(serializedVars);
            Room? room = Room.Get(roomName);
            Client? client = Server.AllClients.FirstOrDefault(e => e.PlayerData.UNToken == apiToken);

            if (client == null) return Unauthorized();

            if (vars == null) return BadRequest("No Data");
            if (room != null)
            {
                if (room.RoomVariables == null) room.RoomVariables = new(); // create a new room variables list for the room if one does not exist
                foreach(var rVar in vars)
                {
                    room.RoomVariables.Add(NetworkArray.VlElement(rVar.Key, rVar.Value, isPersistent: true)); // hardcoding persistance
                }
                NetworkPacket packet = Utils.VlNetworkPacket(room.RoomVariables, room.Id);
                room.Send(packet);
                return Ok();
            } else return BadRequest("Room Not Found");
        }
    } 
}
