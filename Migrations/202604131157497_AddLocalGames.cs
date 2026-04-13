namespace ChessProject.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddLocalGames : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.LocalGameEntities",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Moves = c.String(),
                        Result = c.String(),
                        DatePlayed = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.LocalGameEntities");
        }
    }
}
