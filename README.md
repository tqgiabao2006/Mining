# 🎮 Mining - *A Mining City Builder*

![Blood Vein Logo](https://github.com/tqgiabao2006/Blood-vein/blob/main/ReadMe/MiningLogo.png?raw=true)

[![Unity](https://img.shields.io/badge/Made_with-Unity-000?logo=unity&style=for-the-badge)](https://unity.com/)  
[![GitHub Repo](https://img.shields.io/badge/View_on-GitHub-blue?style=for-the-badge&logo=github)]((https://github.com/tqgiabao2006/Blood-vein))

---

## 🚀 Game Overview  
*Mining* is a **resource management simulation** where you design a **vascular network** to efficiently distribute mining cars underwater. With **Game AI, multi-threading, and procedural generation**, experience the challenge of optimizing pathways using **A* pathfinding and ECS-based logic**.

### 🎯 Key Features
- 🏗 **Road System** – Design organic road networks like blood veins.  
- 🤖 **AI-driven Pathfinding** – Uses **A* algorithm** for vehicle navigation.  
- ⚙️ **Procedural Mesh Generation** – Dynamic road structures adapt to player design.  
- 🔀 **Multi-threading with ECS** – Performance-optimized simulation.  
---

### Details
1.🏗 **Road Systems**
- Grid class:
  + This classed is given a vector 2 of a **map size** to calculate with a constant **node size**
  + Main features: Store data of all current **Node**, return **Node** based on given vector2 position
- Node class:
  + Main property: vector2 Grid Position, bool IsWalkable, float Penalty (to calculate penalty lane), List<Node> Neighbors
  + Main featurs: It stored in a Heap data structure to optimize path finding algorithm
![GridImage]()

*Grid image, with red color indicating a unwalkable node*

![HeapImage]()

*Heap interface to optimize path finding alorigthm*
 
2.🤖 **A-Start Pathfinding Algorithm**
A* (A-Star) is a widely used **graph traversal and pathfinding algorithm** that finds the **shortest path** from a starting point to a target.

**✨ How It Works**
A* combines:  
- **G(n)** → The actual cost from the start node to the current node.  
- **H(n)** → The estimated cost (heuristic) from the current node to the goal.  
- **F(n) = G(n) + H(n)** → The total estimated cost of the path.  

The algorithm **prioritizes nodes with the lowest `F(n)`**, ensuring an optimal and efficient path.  

**🕹 Application in Blood Vein** 
In **Minging**, A* is used for **vehicle movement and network optimization**, allowing mining cars to navigate through the road system efficiently.  

**📌 Why A***  
✔ **Optimal & Efficient** – Finds the shortest path with minimal cost.  
✔ **Heuristic-Based** – Can be tuned for different movement styles.  
✔ **Scalable** – Works for both simple grids and complex road networks.  
✔ **Realistic and Random** - Can be easily editted with some random mistake to make it realistic

3. ⚙️ **Procedural Mesh Generation**
- **Road Mesh**:
  + Pre-calculate 4 standard types of shape with different angles between : 180 degree (Continuous road), 135 degree, 90 degree (Corner road), 45 degree
  + Use enum **Direction** assigned with bitwise interger to merge direction. Iterate through node's neighbor list, and calculate direction between them to get all directions
  + Then, calculate number of standard shape to use, then rotate them to wanted shape
![BitwiseDirection](https://your-image-link.com)
![GetBakedDirections](https://your-image-link.com)

  + Finally, use polar coordinate to create a smooth curve in sharp angle

![CurveMesh](https://your-image-link.com)

- **Parking lot Mesh**:
  + Create a rounded rectangle based on building's size and direction around the building


4. 🔀**Multi-threading with ECS**
- **Why Use It?**
  + **Performance**: With the growing complexity of Mining, I needed a way to handle large amounts of AI-driven entities (mining cars, roads) and data efficiently.
  + **Scalability**: The game simulates a complex environment, and I needed to ensure smooth performance even as the complexity grows over time.
  + **Multithreading**: To avoid performance bottlenecks in critical operations like pathfinding and vehicle movement.
- **How It Was Applied**
- **ECS (Entity Component System)**: I used ECS to decouple game data (position, speed, etc.) from logic, allowing for better memory use and faster CPU processing.
- **Multithreading**: **Multithreading** was implemented to distribute intensive tasks (like pathfinding and vehicle moving updates) across multiple CPU cores, speeding up processing, maintain over **1000 FPS+** even with **1000 cars**
- **Burst Compiler**: Applied **Burst** to optimize performance-critical code (pathfinding and vehicle movement), resulting in highly efficient execution at runtime.

### **Drawbacks**
- **Imperfect**: Despite being powerful, it has some limit, espcially coming with complicated logic with uncertainty data (user-defined data type that change unpredictaly )like spawning buildings. This process requires the involvement of mutliples clases with data may be changed by player (Calculate remaining roads to make sure player can at least create a connection between recently-built houses and others)
- **Complexity**: DOTS requires a different way of thinking about game architecture, which increases the complexity of development. I have been stuck for 2 weeks for the moving mechanics of cars.
- **Debugging**: Multithreading and asynchronous tasks can make debugging more challenging, as race conditions and thread synchronization issues may arise.
 
![ECS](https://your-image-link.com)

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

## 🎮 Current status  
📦 **Developing**

---

## 🚧 Development Roadmap  
🔹 **[ ] Multiplayer Mode** – Co-op city building.  
🔹 **[ ] Improved AI Steering** – Smarter vehicle movement.  
🔹 **[ ] Procedural Environment** – Dynamic terrain growth.  

---

## 🏆 Contributors & Credits  
👨‍💻 **Ben** (*Mad Scientist of Game Lab*) – Solo Developer  
🎵 **Music & SFX:** Open-source / Custom Compositions  
📖 **Special Thanks:** [Unity VietNam Community]  
---

## ⭐ Support & Feedback  
💬 **Have feedback?** Open an [issue](https://github.com/tqgiabao2006/blood-vein/issues) or connect on [Twitter](https://twitter.com/yourhandle).  
🎮 **Follow my journey:** [🔗 Portfolio](https://your-portfolio-link.com)  
