IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'AirportDB')
BEGIN
    CREATE DATABASE AirportDB;
END
GO
USE AirportDB;
GO

IF OBJECT_ID('CapacityCalculations', 'U') IS NOT NULL DROP TABLE CapacityCalculations;
IF OBJECT_ID('Registrations', 'U') IS NOT NULL DROP TABLE Registrations;
IF OBJECT_ID('Flights', 'U') IS NOT NULL DROP TABLE Flights;
IF OBJECT_ID('Passengers', 'U') IS NOT NULL DROP TABLE Passengers;
IF OBJECT_ID('Users', 'U') IS NOT NULL DROP TABLE Users;
IF OBJECT_ID('BoardingGates', 'U') IS NOT NULL DROP TABLE BoardingGates;
IF OBJECT_ID('Aircraft', 'U') IS NOT NULL DROP TABLE Aircraft;
IF OBJECT_ID('Roles', 'U') IS NOT NULL DROP TABLE Roles;
IF OBJECT_ID('FlightStatuses', 'U') IS NOT NULL DROP TABLE FlightStatuses;
IF OBJECT_ID('Terminals', 'U') IS NOT NULL DROP TABLE Terminals;
IF OBJECT_ID('Airlines', 'U') IS NOT NULL DROP TABLE Airlines;
IF OBJECT_ID('Cities', 'U') IS NOT NULL DROP TABLE Cities;
GO

CREATE TABLE Cities (
    CityID INT PRIMARY KEY,
    CityName NVARCHAR(100) NOT NULL
);

CREATE TABLE Airlines (
    AirlineID INT PRIMARY KEY,
    AirlineName NVARCHAR(100) NOT NULL,
    Country NVARCHAR(100) NOT NULL
);

CREATE TABLE Terminals (
    TerminalID INT PRIMARY KEY,
    TerminalNumber NVARCHAR(50) NOT NULL,
    TerminalName NVARCHAR(100) NOT NULL
);

CREATE TABLE FlightStatuses (
    FlightStatusID INT PRIMARY KEY,
    StatusName NVARCHAR(50) NOT NULL
);

CREATE TABLE Roles (
    RoleID INT PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL,
    Description NVARCHAR(255)
);

CREATE TABLE Aircraft (
    AircraftID INT PRIMARY KEY,
    Model NVARCHAR(100) NOT NULL,
    AirlineID INT FOREIGN KEY REFERENCES Airlines(AirlineID),
    PassengerCapacity INT NOT NULL
);

CREATE TABLE BoardingGates (
    GateID INT PRIMARY KEY,
    GateNumber NVARCHAR(50) NOT NULL,
    TerminalID INT FOREIGN KEY REFERENCES Terminals(TerminalID),
    GateStatus NVARCHAR(50),
    IsActive BIT
);

CREATE TABLE Users (
    UserID INT PRIMARY KEY IDENTITY(1,1),
    RoleID INT FOREIGN KEY REFERENCES Roles(RoleID),
    Login NVARCHAR(50) NOT NULL UNIQUE,
    Password NVARCHAR(200) NOT NULL, -- Увеличенная длина для хэша
    LastName NVARCHAR(50),
    FirstName NVARCHAR(50)
);

CREATE TABLE Passengers (
    PassengerID INT PRIMARY KEY IDENTITY(1,1),
    LastName NVARCHAR(50) NOT NULL,
    FirstName NVARCHAR(50) NOT NULL,
    MiddleName NVARCHAR(50),
    Gender NVARCHAR(10),
    BirthDate DATE,
    DocumentType NVARCHAR(50),
    DocumentSeries NVARCHAR(20),
    DocumentNumber NVARCHAR(50)
);

CREATE TABLE Flights (
    FlightID INT PRIMARY KEY IDENTITY(1,1),
    FlightNumber NVARCHAR(50) NOT NULL,
    AirlineID INT FOREIGN KEY REFERENCES Airlines(AirlineID),
    AircraftID INT FOREIGN KEY REFERENCES Aircraft(AircraftID),
    DepartureDateTime DATETIME NOT NULL,
    ArrivalDateTime DATETIME NOT NULL,
    DepartureCityID INT FOREIGN KEY REFERENCES Cities(CityID),
    ArrivalCityID INT FOREIGN KEY REFERENCES Cities(CityID),
    FlightStatusID INT FOREIGN KEY REFERENCES FlightStatuses(FlightStatusID),
    TerminalID INT FOREIGN KEY REFERENCES Terminals(TerminalID),
    GateID INT FOREIGN KEY REFERENCES BoardingGates(GateID)
);

CREATE TABLE Registrations (
    RegistrationID INT PRIMARY KEY IDENTITY(1,1),
    PassengerID INT FOREIGN KEY REFERENCES Passengers(PassengerID),
    FlightID INT FOREIGN KEY REFERENCES Flights(FlightID),
    TicketNumber NVARCHAR(100),
    SeatNumber NVARCHAR(20),
    EmployeeID INT FOREIGN KEY REFERENCES Users(UserID),
    RegistrationDateTime DATETIME NOT NULL
);

CREATE TABLE CapacityCalculations (
    CalculationID INT PRIMARY KEY IDENTITY(1,1),
    CalculationDateTime DATETIME NOT NULL,
    TerminalID INT FOREIGN KEY REFERENCES Terminals(TerminalID),
    NumberOfCounters INT,
    ArrivalRatePerHour INT,
    AverageServiceTimeMinutes INT,
    ServiceRatePerCounterPerHour INT,
    TotalServiceCapacityPerHour INT,
    Utilization FLOAT,
    ProbabilityOfWaiting FLOAT,
    AverageQueueLength FLOAT,
    AverageWaitMinutes FLOAT,
    QueueModel NVARCHAR(10)
);
GO

INSERT INTO Cities (CityID, CityName) VALUES
(1, N'Москва'), (2, N'Санкт-Петербург'), (3, N'Казань'), (4, N'Сочи'),
(5, N'Екатеринбург'), (6, N'Париж'), (7, N'Стамбул'), (8, N'Минск');

INSERT INTO Airlines (AirlineID, AirlineName, Country) VALUES
(1, N'Аэрофлот', N'Россия'), (2, N'S7 Airlines', N'Россия'), (3, N'Air France', N'Франция'),
(4, N'Turkish Airlines', N'Турция'), (5, N'Belavia', N'Беларусь');

INSERT INTO Terminals (TerminalID, TerminalNumber, TerminalName) VALUES
(1, 'T1', N'Терминал внутренних рейсов'),
(2, 'T2', N'Международный терминал'),
(3, 'T3', N'Дополнительный терминал');

INSERT INTO FlightStatuses (FlightStatusID, StatusName) VALUES
(1, N'По расписанию'), (2, N'Регистрация'), (3, N'Посадка'),
(4, N'Задержан'), (5, N'Отменён'), (6, N'Завершён');

INSERT INTO Roles (RoleID, RoleName, Description) VALUES
(1, N'Администратор', N'Управление системой и справочниками'),
(2, N'Агент по регистрации', N'Регистрация пассажиров на рейсы'),
(3, N'Аналитик', N'Расчёт пропускной способности и анализ данных'),
(4, N'Директор', N'Просмотр отчётов и показателей аэропорта');

INSERT INTO Aircraft (AircraftID, Model, AirlineID, PassengerCapacity) VALUES
(1, N'Airbus A320', 1, 180), (2, N'Airbus A321', 1, 220),
(3, N'Boeing 737-800', 2, 189), (4, N'Airbus A320', 3, 174),
(5, N'Airbus A321', 4, 200), (6, N'Embraer 175', 5, 76);

INSERT INTO BoardingGates (GateID, GateNumber, TerminalID, GateStatus, IsActive) VALUES
(1, '01', 1, N'Свободен', 1), (2, '02', 1, N'Занят', 1), (3, '03', 1, N'Свободен', 1),
(4, '04', 1, N'На обслуживании', 1), (5, '05', 2, N'Свободен', 1), (6, '06', 2, N'Занят', 1),
(7, '07', 2, N'Свободен', 1), (8, '08', 2, N'Свободен', 1), (9, '09', 3, N'Свободен', 1),
(10, '10', 3, N'Свободен', 1);

-- Пароли захэшированы (1234, 5678, 9012, 1111)
INSERT INTO Users (RoleID, Login, Password, LastName, FirstName) VALUES
(1, 'admin', '03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4', N'Иванов', N'Иван'),
(2, 'manager', '7c222fb2927d828af22f592134e8932480637c0d07c222fb2927d828af22f5921', N'Петров', N'Петр'),
(3, 'guest', 'c775e7b757ede630cd0aa1113bd102661ab38829c775e7b757ede630cd0aa1113', N'Сидоров', N'Сидор'),
(4, 'user', '0ffe1abd1a0682221c7b5c1c0b1ed66d1d0e8d4a0ffe1abd1a0682221c7b5c1c', N'Смирнов', N'Алексей');

INSERT INTO Passengers (LastName, FirstName, MiddleName, Gender, BirthDate, DocumentType, DocumentSeries, DocumentNumber) VALUES
(N'Иванов', N'Александр', N'Сергеевич', N'М', '1995-04-12', N'Паспорт', N'РФ', N'4501 123456'),
(N'Петрова', N'Мария', N'Игоревна', N'Ж', '1998-07-23', N'Паспорт', N'РФ', N'4502 234567'),
(N'Смирнов', N'Дмитрий', N'Олегович', N'М', '1987-02-15', N'Паспорт', N'РФ', N'4503 345678'),
(N'Кузнецова', N'Анна', N'Викторовна', N'Ж', '2000-11-08', N'Паспорт', N'РФ', N'4504 456789'),
(N'Соколов', N'Павел', N'Андреевич', N'М', '1992-06-19', N'Паспорт', N'РФ', N'4505 567890');

SET IDENTITY_INSERT Flights ON;
INSERT INTO Flights (FlightID, FlightNumber, AirlineID, AircraftID, DepartureDateTime, ArrivalDateTime, DepartureCityID, ArrivalCityID, FlightStatusID, TerminalID, GateID) VALUES
(1, 'SU101', 1, 1, '2026-11-07 08:00:00', '2026-11-07 10:00:00', 1, 2, 2, 1, 1),
(2, 'S7102', 2, 3, '2026-11-07 09:00:00', '2026-11-07 11:00:00', 1, 3, 1, 1, 3),
(3, 'AF103', 3, 4, '2026-11-07 10:30:00', '2026-11-07 13:30:00', 1, 6, 4, 2, 5),
(4, 'TK104', 4, 5, '2026-11-07 12:00:00', '2026-11-07 15:00:00', 1, 7, 3, 2, 6),
(5, 'B2105', 5, 6, '2026-11-07 14:00:00', '2026-11-07 16:00:00', 1, 8, 1, 2, 7);
SET IDENTITY_INSERT Flights OFF;

INSERT INTO Registrations (PassengerID, FlightID, TicketNumber, SeatNumber, EmployeeID, RegistrationDateTime) VALUES
(1, 1, 'TEST001', '01A', 2, '2026-11-07 06:30:00'),
(2, 1, 'TEST002', '01B', 2, '2026-11-07 06:35:00'),
(3, 2, 'TEST003', '02A', 3, '2026-11-07 07:45:00');

INSERT INTO CapacityCalculations (CalculationDateTime, TerminalID, NumberOfCounters, ArrivalRatePerHour, AverageServiceTimeMinutes, ServiceRatePerCounterPerHour, TotalServiceCapacityPerHour, Utilization, ProbabilityOfWaiting, AverageQueueLength, AverageWaitMinutes, QueueModel) VALUES
('2026-11-06 10:00:00', 1, 2, 30, 3, 20, 40, 0.7500, 0.6429, 1.929, 0.25, 'M'),
('2026-11-06 11:00:00', 1, 3, 45, 2, 30, 90, 0.5000, 0.2368, 0.237, 0.32, 'M'),
('2026-11-06 12:00:00', 2, 4, 60, 2, 24, 96, 0.6250, 0.3199, 0.533, 0.53, 'M');
GO