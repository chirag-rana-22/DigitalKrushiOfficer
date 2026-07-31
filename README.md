# 🌾 Digital Krushi Officer

An AI-powered smart agriculture platform that helps farmers make informed decisions using artificial intelligence, weather forecasting, crop recommendations, pest detection, and government scheme information. The application is designed to bridge the gap between farmers and agricultural experts through a modern, user-friendly web interface.

---

## 📌 Features

* 🤖 AI-powered farming assistant
* 🌦️ Real-time weather information
* 🌱 Crop recommendation system
* 🐛 Pest and disease identification
* 📷 Image upload for crop analysis
* 👨‍🌾 Farmer and Agriculture Officer dashboards
* 📢 Government scheme and subsidy information
* 📈 Crop management and monitoring
* 📧 Email notifications using SMTP
* 🔐 Secure user authentication and authorization
* 📱 Responsive design for desktop and mobile devices

---

## 🛠️ Tech Stack

### Frontend

* HTML5
* CSS3
* Bootstrap 5
* JavaScript

### Backend

* ASP.NET Core MVC (.NET 10)

### Database

* MySQL

### ORM

* Entity Framework Core

### APIs

* Google Gemini AI API
* OpenWeather API

---

## 📂 Project Structure

```text
DigitalKrushiOfficer/
│── Controllers/
│── Models/
│── Views/
│── Services/
│── Data/
│── wwwroot/
│── appsettings.json
│── Program.cs
└── README.md
```

---

## 🚀 Installation

### 1. Clone the repository

```bash
git clone https://github.com/chirag-rana-22/DigitalKrushiOfficer.git
```

### 2. Navigate to the project

```bash
cd DigitalKrushiOfficer
```

### 3. Restore packages

```bash
dotnet restore
```

### 4. Configure the database

Update the connection string in `appsettings.json`.

### 5. Configure API Keys

Replace the placeholder values in `appsettings.json`:

* Gemini API Key
* OpenWeather API Key
* SMTP Username
* SMTP Password

> **Important:** Never commit real API keys or passwords to GitHub.

### 6. Run the project

```bash
dotnet run
```

The application will be available at:

```text
https://localhost:5001
```

or

```text
http://localhost:5000
```

---

## 📸 Modules

* Home
* Farmer Registration & Login
* Officer Registration & Login
* AI Chat Assistant
* Weather Dashboard
* Crop Recommendation
* Pest & Disease Detection
* Government Schemes
* Profile Management
* Email Verification

---

## 🔒 Security

* Password hashing
* Authentication & Authorization
* Input validation
* Secure session handling
* API keys stored in configuration (not in source control)

---

## 📈 Future Enhancements

* Voice-based chatbot
* Regional language support
* Satellite imagery integration
* Live market prices
* Mobile application
* SMS notifications
* AI-based crop yield prediction

---

## 👨‍💻 Developed By

**Chirag Rana**

Final Year Project

---

## 📄 License

This project is developed for educational and academic purposes.
