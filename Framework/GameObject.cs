using System;
using System.Collections.Generic;
using Silk.NET.Maths;

namespace ef2dg
{
    public class GameObject
    {
        public string Name { get; set; } = "New GameObject";
        public Vector2D<float> Position { get; set; } = new Vector2D<float>(0, 0);
        public Vector2D<float> Scale { get; set; } = new Vector2D<float>(1, 1);
        public float Rotation { get; set; } = 0f;
        public Scene ParentScene { get; set; } = null!;
        public bool AlwaysActive { get; set; } = false;
        public bool IsUI { get; set; } = false;
        private Dictionary<Type, Component> _components = new Dictionary<Type, Component>();

        public T AddComponent<T>(T component) where T : Component
        {
            component.Owner = this;
            
            _components[typeof(T)] = component; // O(1) запис
            
            component.Start();
            return component;
        }

        public T? GetComponent<T>() where T : Component
        {
            if (_components.TryGetValue(typeof(T), out var component))
            {
                return (T)component; // O(1) пошук
            }
            return null;
        }

        public void RemoveComponent(Component component)
        {
            // ВАЖНО: Если твой внутренний список компонентов называется не _components, 
            // а, например, Components, замени название здесь:
            if (component != null)
            {
                _components.Remove(component.GetType()); 
            }
        }

        // Метод для сериализатора
        public void AddComponentDynamic(Component component, Type type)
        {
            component.Owner = this;
            _components[type] = component; // Сохраняем в словарь по типу
            component.Start();
        }

        // Выдает список всех компонентов, которые висят на этом объекте
        public System.Collections.Generic.IEnumerable<Component> GetAllComponents() => _components.Values;

        public void OnTriggerEnter(GameObject other)
        {
            foreach (var component in _components.Values)
            {
                component.OnTriggerEnter(other);
            }
        }

        public void OnTriggerExit(GameObject other)
        {
            foreach (var component in _components.Values)
            {
                component.OnTriggerExit(other);
            }
        }
        
        public void OnTriggerStay(GameObject other)
        {
            foreach (var component in _components.Values)
            {
                component.OnTriggerStay(other);
            }
        }
        
        public void FixedUpdate(float fixedDeltaTime)
        {
            foreach (var component in _components.Values)
            {
                component.FixedUpdate(fixedDeltaTime);
            }
        }

        public void Update(float deltaTime)
        {
            // Перебираем только значения словаря
            foreach (var component in _components.Values)
            {
                component.Update(deltaTime);
            }
        }

        public void Draw(SpriteRenderer renderer, Camera camera)
        {
            foreach (var component in _components.Values)
            {
                component.Draw(renderer, camera);
            }
        }
    }
}