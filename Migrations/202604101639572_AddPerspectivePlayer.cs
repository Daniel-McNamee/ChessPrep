namespace ChessProject.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPerspectivePlayer : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.GameEntities", "PerspectivePlayer", c => c.String());
            AddColumn("dbo.RecentGameEntities", "PerspectivePlayer", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.RecentGameEntities", "PerspectivePlayer");
            DropColumn("dbo.GameEntities", "PerspectivePlayer");
        }
    }
}
