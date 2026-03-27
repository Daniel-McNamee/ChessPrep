using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessProject.Entities
{
    public class FavouritePlayerEntity
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public DateTime DateAdded { get; set; }
    }
}
