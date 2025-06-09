# Unity Paint Project

<div align="center">
  <img align="center" class="header-icon" src="./public/imgs/icon-imgs/lcj-icon.jpg" alt="icon" />
  <p>Author: Yar Crasy (LINGCHENG JIANG)</p>
</div>

## FIRST TIME SETUP
This project is made in Unity 6000.0.50f1 LTS (2D Built-in RP Template).  
To run the project correctly for the first time, make sure you:

1. Open the project in Unity Hub using the **2022.3.x LTS version**.
2. Install **MySQL** on your machine (if not already installed).
3. Add the `MySql.Data.dll` to the project if missing (for DB connection).
4. Ensure your MySQL server is running and the connection data (user/password/port) is correctly set in the Unity script.

## About the Project
<p>
 This Unity project is part of a school assignment and aims to replicate a simplified version of Microsoft Paint. 
 Users can draw various shapes, select colors, and store the resulting artwork in a MySQL database.
</p>

## Built with
For now, the project is built using:
* Unity (2022.3.x LTS)
* C# (.NET scripting backend)
* MySQL (local server)
* JSON (for point data serialization)

## Composition
The application use the MVC standar, that mean you can find in the project:
* **Model**: Where you can find the structure of the data models
* **UI (views)**: scripts to control the Graphic part of the software
* **Controller**: Handles all the logic of the software

## Third-Party Components
This project uses:
* [MySql.Data](https://dev.mysql.com/downloads/connector/net/) – .NET Connector for MySQL
* Unity’s built-in LineRenderer

## Inspired in
<p>The project is conceptually inspired by the traditional Windows Paint but adapted for educational usage project using database integration.</p> 

## Bibliography
<p>Unity + MySQL: <a href="https://stackoverflow.com/questions/49940752/how-to-connect-unity-with-mysql-database">StackOverflow</a></p>
<p>Basic Paint in Unity: <a href="https://www.youtube.com/results?search_query=unity+paint+app">YouTube Tutorials</a></p>
<p>MySQL JSON Serialization: <a href="https://www.newtonsoft.com/json">Newtonsoft.Json</a> (if used)</p>
