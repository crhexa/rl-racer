# RL-Racer

A custom environment made using the [Godot Engine](https://github.com/godotengine/godot) and written in C# used to train reinforcement learning models to drive a car on a top-down virtual track with semi-realistic physics. Utilizes the [Godot RL Agents](https://github.com/edbeeching/godot_rl_agents) framework to train agents using the Stable-Baselines3 implementation of OpenAI's [Proximal Policy Optimization](https://openai.com/research/openai-baselines-ppo) algorithm.


## Demo - Environment v0.1
### Model 1


https://github.com/crhexa/rl-racer/assets/56240785/85e6021e-c956-466e-8bcd-cc6dcbf13bb3

## Demo - Environment v0.2
### Model 2



https://github.com/crhexa/rl-racer/assets/56240785/c8e3b368-0f54-4dbf-bb4f-2ab33c48e25b

[Full Demo Video](https://youtu.be/TICGJOJwRHg)


## Demo - Environment v0.3
### Model 3



https://github.com/crhexa/rl-racer/assets/56240785/d42c547b-51e6-4a73-bfd1-3ce8472242d3

[Full Demo Video](https://youtu.be/zaZSQkXxg2c)


## Installation

Work In Progress


## Changelog
### Environment v0.3:
- Added training of multiple agents
- Added multi-target camera tracking

### Environment v0.2:
- Added an angle limit to reach fail state
- Added a small penalty for driving off-track
- Increased off-track friction
- Slightly increased slip speed threshold

## Roadmap
- Time-tracking and checkpoints
- Human controlled vs. Agent(s) controlled
- Improved collision avoidance

