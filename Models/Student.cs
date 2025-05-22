// Models/Student.cs
public class Student
{
    public int StudentId { get; set; }
    public string FullName { get; set; }
    public int ClassId { get; set; }
    public Class Class { get; set; }
}
