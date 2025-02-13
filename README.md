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
- 🎨 **Stylized Visuals** – Unique underwater city aesthetic.  

---

### Details
1. **Road Systems**
- Grid class:
  + This classed is given a vector 2 of a **map size** to calculate with a constant **node size**
  + Main features: Store data of all current **Node**, return **Node** based on given vector2 position
- Node class:
  + Main property: vector2 Grid Position, bool IsWalkable, float Penalty (to calculate penalty lane)
  + Main featurs: It stored in a Heap data structure to optimize path finding algorithm
[GridImage]()

*Grid image, with red color indicating a unwalkable node*

[HeapImage]()

*Heap interface to optimize path finding alorigthm*
 
2. **A-Start Pathfinding Algorithm**
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




 
  

## 🖼 Screenshots  
| ![Screenshot1](https://your-image-link.com) | ![Screenshot2](https://your-image-link.com) |  
|:----------------------------------:|:----------------------------------:|  

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

## 🎮 Try It Out  
📦 **Download the latest build:** [🔗 Itch.io](https://your-itchio-link.com)  
🕹 **Play the WebGL Demo:** [🌐 Live Version](https://your-live-demo-link.com)  

---

## 🚧 Development Roadmap  
🔹 **[ ] Multiplayer Mode** – Co-op city building.  
🔹 **[ ] Improved AI Steering** – Smarter vehicle movement.  
🔹 **[ ] Procedural Environment** – Dynamic terrain growth.  

---

## 🏆 Contributors & Credits  
👨‍💻 **Ben** (*Mad Scientist of Game Lab*) – Solo Developer  
🎵 **Music & SFX:** Open-source / Custom Compositions  
📖 **Special Thanks:** [Game Dev Community]  

---

## 📜 License  
This project is licensed under the **MIT License** – see the [LICENSE](LICENSE) file for details.  

---

## ⭐ Support & Feedback  
💬 **Have feedback?** Open an [issue](https://github.com/yourname/blood-vein/issues) or connect on [Twitter](https://twitter.com/yourhandle).  
🎮 **Follow my journey:** [🔗 Portfolio](https://your-portfolio-link.com)  
