

A comprehensive web-based school management system designed specifically for primary schools, built with ASP.NET Core. This system streamlines administrative tasks, student management, and academic tracking with robust security features and an intuitive user interface.

## 🎯 Overview

The School Management System provides a complete solution for managing primary school operations, featuring role-based access control for administrators and teachers. The system emphasizes security, usability, and comprehensive reporting capabilities.

## ✨ Key Features

### 🔐 Security & Authentication
- *User Authentication* - Secure login system with role-based access
- *Two-Factor Authentication (2FA)* - Enhanced security for all user accounts
- *Data Encryption* - End-to-end encryption for sensitive data and files
- *Secure File Downloads* - Protected file access with proper authorization

### 👨‍💼 Administrator Features
- *Complete System Control* - Full administrative privileges across all modules
- *Class Management* - Create, edit, and organize school classes
- *Subject Management* - Manage subjects and assign them to appropriate classes
- *Student Management* - Register students and assign them to their respective classes
- *Teacher Management* - Manage teacher accounts and provide system credentials
- *Academic Oversight* - Review and manage student marks entered by teachers
- *Comprehensive Reporting* - Access school-wide reports including:
  - Attendance tracking and analysis
  - School performance metrics
  - Individual student transcripts
  - Academic progress reports

### 👩‍🏫 Teacher Features
- *Attendance Management* - Mark daily student attendance with real-time admin notifications
- *Grade Recording* - Record and manage student marks and assessments
- *Attendance Reports* - View detailed attendance reports for assigned classes
- *Student Progress Tracking* - Monitor individual student academic progress

### 📊 Advanced Capabilities
- *Analytics & Monitoring* - Real-time system analytics and performance monitoring
- *Customizable Reports* - Generate tailored reports based on specific requirements
- *Notification System* - Automated notifications for important events and updates
- *User Interface Customization* - Adaptable interface to meet different user preferences

## 🛠 Technology Stack

- *Framework*: ASP.NET Core
- *Development Environment*: Visual Studio 2022
- *Database*:  SQL Server
- *Frontend*: HTML5, CSS3, JavaScript, Bootstrap
- *Authentication*: ASP.NET Core Identity with 2FA
- *Security*: Data encryption, secure authentication protocols

## 🚀 Getting Started

### Prerequisites
- Visual Studio 2022
- .NET Core SDK 8
- SQL Server
- Git

### Installation

1. *Clone the repository*
   bash
   git clone https://github.com/ishimwe-pacifique/SCHOOL-MANAGEMENT-SYSTEM-DOT-NET-PROJECT.git 
  cd school-management-system
   

2. *Open in Visual Studio*
   - Launch Visual Studio
   - Open the .sln solution file

3. *Configure Database*
   - Update the connection string in appsettings.json
   - Run database migrations:
   bash
   dotnet ef database update
   

4. *Build and Run*
   bash
   dotnet build
   dotnet run
   

5. *Access the Application*
   - Navigate to https://localhost:5001 (or your configured port)
   - Use the default admin credentials to get started

## 👥 User Roles

### Administrator
- Full system access and control
- Manage all users, classes, subjects, and students
- Generate comprehensive reports
- System configuration and maintenance

### Teacher
- Mark student attendance
- Record student grades and assessments
- View attendance reports for assigned classes
- Monitor student academic progress

## 📋 System Requirements

- *Server*: Windows Server 2016+ or Linux
- *Database*: SQL Server 2017+ (or compatible database)
- *Browser*: Modern web browsers (Chrome, Firefox, Edge, Safari)
- *RAM*: Minimum 4GB, Recommended 8GB+
- *Storage*: Minimum 10GB available space

## 🔧 Configuration

### Database Setup
1. Create a new database for the application
2. Update the connection string in appsettings.json
3. Run the initial migration to set up tables

### Security Configuration
- Configure 2FA settings in the application settings
- Set up encryption keys for data protection
- Configure secure file storage locations

## 📈 Features in Detail

### Attendance Management
- Real-time attendance marking
- Automated notifications to administrators
- Comprehensive attendance reports and analytics

### Academic Management
- Subject assignment to classes
- Grade recording and management
- Student transcript generation
- Performance analytics

### Reporting System
- Customizable report generation
- Export capabilities (PDF, Excel)
- Real-time data visualization
- Historical data analysis

## 🤝 Contributing

We welcome contributions to improve the School Management System. Please follow these steps:

1. Fork the repository
2. Create a feature branch (git checkout -b feature/AmazingFeature)
3. Commit your changes (git commit -m 'Add some AmazingFeature')
4. Push to the branch (git push origin feature/AmazingFeature)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 📞 Support

For support and questions:
- Create an issue on GitHub
- Email: [mbznolla40@gmail.com]


## 🎉 Acknowledgments

- Thanks to all contributors who helped build this system
- Special recognition to the educational institutions that provided feedback
- ASP.NET Core community for excellent documentation and support

---

*Built with ❤ for education by Group 6
