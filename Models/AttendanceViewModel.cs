// Models/AttendanceViewModel.cs
public class AttendanceViewModel
{
    public int ClassId { get; set; }
    public string ClassName { get; set; }
    public List<StudentAttendance> Students { get; set; }
}

public class StudentAttendance
{
    public int StudentId { get; set; }
    public string FullName { get; set; }
    public bool IsPresent { get; set; }
}
