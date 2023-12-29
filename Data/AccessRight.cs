namespace _4PL.Data
{
    public class AccessRight
    {
        public string? AccessType { get; set; } = "";
        public string? UpdatedAccessType { get; set; } = "";
        public bool OriginalRight { get; set; } = false;
        public bool UpdatedRight { get; set; } = false;
        public string? Description { get; set; } = "";
        public string? UpdatedDescription { get; set; } = "";

        public void reset()
        {
            UpdatedRight = OriginalRight;
        }
    }
}
