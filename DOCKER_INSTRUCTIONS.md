# CARS Platform - Docker Guide 🐳

If you are sharing this code with a friend, they do **not** need to install Visual Studio, .NET SDK, or SQL Server on their computer. They only need **Docker**. 

Here is exactly what they need and the steps to run it fully contained.

## 📦 What your friend needs to install first:
1. **Docker Desktop**: [Download here](https://www.docker.com/products/docker-desktop/) (Available for Windows, Mac, and Linux).
2. Ensure Docker Desktop is open and running in the background.

## 🚀 How to Run the App (Step-by-Step)

### Step 1: Open the Terminal
Open a terminal (Command Prompt, PowerShell, or Terminal on Mac) and navigate to the folder where the `CARS` code is located.
```bash
cd path/to/CARS
```

### Step 2: Spin Up the Containers
Run the following single magical command:
```bash
docker-compose up --build
```

**What this command does automatically behind the scenes:**
1. Downloads a Linux SQL Server container and starts it.
2. Downloads a Redis cache container and starts it.
3. Builds the `CARS` API itself using the exact .NET 10 version we used.
4. Starts the API, automatically applies database migrations to the SQL Server, and serves the frontend dashboard to the web.

### Step 3: Open the App!
Once the terminal logs calm down and you see `Application started. Press Ctrl+C to shut down.`, open your browser to:
👉 **[http://localhost:5000/index.html](http://localhost:5000/index.html)**

---

## 🛑 How to Stop the App
When you are done testing, simply go back to your terminal window and press:
`Ctrl + C`
Then type:
```bash
docker-compose down
```
*(This gracefully shuts down the server, database, and cache without losing your data).*

## 🧹 How to Reset Everything (Wipe Data)
If you run into database errors or want to start completely fresh without any test data, use:
```bash
docker-compose down -v
```
*(The `-v` tag completely deletes the volumes, wiping all SQL data and uploaded documents).*
