namespace ChessProject.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.FavouritePlayerEntities",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Username = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.GameEntities",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        WhitePlayer = c.String(),
                        BlackPlayer = c.String(),
                        WhiteElo = c.Int(nullable: false),
                        BlackElo = c.Int(nullable: false),
                        Result = c.String(),
                        PGN = c.String(),
                        Date = c.DateTime(nullable: false),
                        TimeControl = c.String(),
                        IsFavourite = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.MoveNoteEntities",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        GameId = c.Int(nullable: false),
                        MoveIndex = c.Int(nullable: false),
                        Note = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.GameEntities", t => t.GameId, cascadeDelete: true)
                .Index(t => t.GameId);
            
            CreateTable(
                "dbo.LocalGameEntities",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        WhitePlayer = c.String(),
                        BlackPlayer = c.String(),
                        Result = c.String(),
                        PGN = c.String(),
                        DatePlayed = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.OpeningEntities",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        ECO = c.String(),
                        Moves = c.String(),
                        IsFavourite = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.SavedPositionEntities",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        FEN = c.String(),
                        Notes = c.String(),
                        DateSaved = c.DateTime(nullable: false),
                        SourceGame = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.MoveNoteEntities", "GameId", "dbo.GameEntities");
            DropIndex("dbo.MoveNoteEntities", new[] { "GameId" });
            DropTable("dbo.SavedPositionEntities");
            DropTable("dbo.OpeningEntities");
            DropTable("dbo.LocalGameEntities");
            DropTable("dbo.MoveNoteEntities");
            DropTable("dbo.GameEntities");
            DropTable("dbo.FavouritePlayerEntities");
        }
    }
}
