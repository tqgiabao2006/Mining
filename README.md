# 🎮 Mining - *A Mining City Builder*

![Blood Vein Logo](https://github.com/tqgiabao2006/Blood-vein/blob/main/ReadMe/MiningLogo.png?raw=true)

[![Unity](https://img.shields.io/badge/Made_with-Unity-000?logo=unity&style=for-the-badge)](https://unity.com/)  
[![GitHub Repo](https://img.shields.io/badge/View_on-GitHub-blue?style=for-the-badge&logo=github)](https://github.com/tqgiabao2006/Blood-vein)

---

## 🚀 Game Overview  
*Mining* is a **resource management simulation** where you design a **vascular network** to efficiently distribute mining cars underwater. With **Game AI, multi-threading, and procedural generation**, experience the challenge of optimizing pathways using **A* pathfinding and ECS-based logic**.

### 🎯 Key Features
- 🏗 **Road System** – Design organic road networks like blood veins.  
- 🤖 **AI-driven Pathfinding** – Uses the **A* algorithm** for vehicle navigation.  
- ⚙️ **Procedural Mesh Generation** – Dynamic road structures adapt to player design.  
- 🔀 **Multi-threading with ECS** – Performance-optimized simulation.  
---

### 📌 Details

#### **1. 🏗 Road Systems**
- **Grid Class:**
  - This class is given a Vector2 of a **map size** to calculate with a constant **node size**.
  - Main features: Stores data of all current **Nodes** and returns a **Node** based on the given Vector2 position.
- **Node Class:**
  - Main properties: `Vector2 Grid Position`, `bool IsWalkable`, `float Penalty` (for penalty lanes), `List<Node> Neighbors`.
  - Stored in a **Heap** data structure to optimize the pathfinding algorithm.

![Grid Image](https://github.com/tqgiabao2006/Blood-vein/raw/main/ReadMe/BloodVein_Grid.png)

*Grid image, with red color indicating an unwalkable node.*

![Heap Image](https://github.com/tqgiabao2006/Blood-vein/raw/main/ReadMe/Heap.png)

*Heap interface to optimize the pathfinding algorithm.*
 
---

#### **2. 🤖 A* Pathfinding Algorithm**
A* (A-Star) is a widely used **graph traversal and pathfinding algorithm** that finds the **shortest path** from a starting point to a target.

**✨ How It Works**
A* combines:
- **G(n)** → The actual cost from the start node to the current node.
- **H(n)** → The estimated cost (heuristic) from the current node to the goal.
- **F(n) = G(n) + H(n)** → The total estimated cost of the path.

The algorithm **prioritizes nodes with the lowest `F(n)`**, ensuring an optimal and efficient path.  

**🕹 Application in Blood Vein**  
In **Mining**, A* is used for **vehicle movement and network optimization**, allowing mining cars to navigate efficiently.

**📌 Why A***  
✔ **Optimal & Efficient** – Finds the shortest path with minimal cost.  
✔ **Heuristic-Based** – Can be tuned for different movement styles.  
✔ **Scalable** – Works for both simple grids and complex road networks.  
✔ **Realistic and Random** – Can be modified with random variations to create more realistic behavior.  

---

#### **3. ⚙️ Procedural Mesh Generation**
- **Road Mesh:**
  - Pre-calculates four standard shape types with different angles: 180° (continuous road), 135°, 90° (corner road), and 45°.
  - Uses an enum **Direction**, assigned with bitwise integers to merge directions. Iterates through a node's neighbor list and calculates the direction between them.
  - Determines the number of standard shapes required, then rotates them accordingly.

![Bitwise Direction](https://github.com/tqgiabao2006/Blood-vein/raw/main/ReadMe/Enum%20Direction.png)

![Get Baked Directions](https://github.com/tqgiabao2006/Blood-vein/raw/main/ReadMe/Get%20direction.png)

  - Finally, uses polar coordinates to create smooth curves at sharp angles.

![Curve Mesh](https://github.com/tqgiabao2006/Blood-vein/raw/main/ReadMe/Smooth%20curve.png)

- **Parking Lot Mesh:**
  - Creates a rounded rectangle based on the building's size and direction.

---

#### **4. 🔀 Multi-threading with ECS**
- **Why Use It?**
  - **Performance**: Needed to handle large amounts of AI-driven entities (mining cars, roads) efficiently.
  - **Scalability**: Ensures smooth performance as complexity grows.
  - **Multithreading**: Avoids performance bottlenecks in pathfinding and vehicle movement.

- **How It Was Applied:**
  - **ECS (Entity Component System)**: Decouples game data (position, speed, etc.) from logic, improving memory usage and CPU performance.
  - **Multithreading**: Distributes intensive tasks across multiple CPU cores, maintaining **1000+ FPS** even with **1000 cars**.
  - **Burst Compiler**: Optimizes performance-critical code, improving runtime execution efficiency.

### **Drawbacks**
- **Complexity**: DOTS requires a new approach to game architecture, increasing development difficulty.
- **Debugging Challenges**: Multithreading introduces race conditions and synchronization issues.
- **Imperfect for Uncertain Data**: User-defined data types that change unpredictably can cause issues in road calculations and building placement.

![ECS](https://github.com/tqgiabao2006/Blood-vein/raw/main/ReadMe/ECS.png)

---
## 🎥 Demo Gameplay Video
![Gameplay Preview](https://github.com/tqgiabao2006/Blood-vein/raw/main/ReadMe/Gameplay.gif)

---

## 🛠 Tech Stack  
| **Technology**   | **Usage**  |  
|-----------------|-----------|  
| Unity (C#) | Core Engine & Gameplay |  
| Shader Graph | Visual Effects & Water Rendering |  
| A* Algorithm | Pathfinding |  
| ECS (Entity Component System) | Multi-threading Performance |  

---

## 🏗 Design Patterns Used  
✔ **Observer Pattern** – Event-driven architecture for game logic.  
✔ **State Pattern** – AI and game state transitions.  
✔ **Factory Pattern** – Dynamic object creation.  
✔ **Unity Test Framework** – Ensures stability and correctness.  

---

## 🎮 Current Status  
📦 **Developing**

---

## 🚧 Development Roadmap  
🔹 **[ ] Multiplayer Mode** – Co-op city building.  
🔹 **[ ] Improved AI Steering** – Smarter vehicle movement.  
🔹 **[ ] Procedural Environment** – Dynamic terrain growth.  
🔹 **[ ] Transition to 3D Perspective**  

---

## 🏆 Contributors & Credits  
👨‍💻 **Ben** (*Mad Scientist of Game Lab*) – Solo Developer  
🎵 **Music & SFX:** Open-source / Custom Compositions  
📖 **Special Thanks:** [Unity Vietnam Community], and Senior Game Artist Tung Anh as an advisor  

---

## ⭐ Support & Feedback  
💬 **Have feedback?** Open an [issue](https://github.com/tqgiabao2006/blood-vein/issues) or contact me via email: tqgiabao2006@gmail.com.  
🎮 **Follow my journey:** [🔗 Portfolio](https://your-portfolio-link.com)  
