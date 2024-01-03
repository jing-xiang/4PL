namespace _4PL.Data
{
    public class UserProfileLayout
    {
        public string User_Email { get; set; } = "";
        public string Table_Name { get; set; } = "";
        public string Layout_Name { get; set; } = "";
        public string[] Layout_Fields { get; set; } = [];
        public bool Is_Default { get; set; } = false;
    }
}
