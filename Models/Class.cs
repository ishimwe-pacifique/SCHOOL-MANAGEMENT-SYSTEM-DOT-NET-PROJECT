// Models/Class.cs
using School_Management_System.Pages;

public class Class
{
    public int ClassId { get; set; }
    public string ClassName { get; set; }

    public ICollection<Student> Students { get; set; }
}
