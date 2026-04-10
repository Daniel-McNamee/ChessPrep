namespace ChessProject.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemoveLocalGameEntity : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.GameEntities", "IsLocalGame", c => c.Boolean(nullable: false));
            DropTable("dbo.LocalGameEntities");
        }
        
        public override void Down()
        {
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
            
            DropColumn("dbo.GameEntities", "IsLocalGame");
        }
    }
}
