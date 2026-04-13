using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChessProject.Entities;
using ChessProject.Models;
using System.Data.Entity;

namespace ChessProject.Data
{
    public class ChessDbContext : DbContext
    {
        public ChessDbContext() : base("ChessDb")
        {
        }

        public DbSet<GameEntity> Games { get; set; }
        public DbSet<OpeningEntity> FavouriteOpenings { get; set; }
        public DbSet<FavouritePlayerEntity> FavouritePlayers { get; set; }
        public DbSet<MoveNoteEntity> MoveNotes { get; set; }
        public DbSet<RecentGameEntity> RecentGames { get; set; }
        public DbSet<SavedPositionEntity> SavedPositions { get; set; }
        public DbSet<LocalGameEntity> LocalGames { get; set; }
    }
}
