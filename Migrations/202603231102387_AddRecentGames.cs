namespace ChessProject.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddRecentGames : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.RecentGameEntities",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        WhitePlayer = c.String(),
                        BlackPlayer = c.String(),
                        WhiteElo = c.Int(nullable: false),
                        BlackElo = c.Int(nullable: false),
                        Result = c.String(),
                        PGN = c.String(),
                        DateViewed = c.DateTime(nullable: false),
                        TimeControl = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.RecentGameEntities");
        }
    }
}
