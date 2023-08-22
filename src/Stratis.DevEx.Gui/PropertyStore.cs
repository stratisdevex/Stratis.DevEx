using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using System;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Stratis.DevEx.Gui
{
    public class PropertyStore : ConcurrentDictionary<object, object>
    {
        /// <summary>
        /// Gets the parent object that this property store is attached to
        /// </summary>
        /// <remarks>
        /// This is used to attach/remove events
        /// </remarks>
        /// <value>The parent object</value>
        public object Parent { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Eto.PropertyStore"/> class.
        /// </summary>
        /// <param name="parent">Parent to attach the properties to</param>
        public PropertyStore(object parent)
        {
            this.Parent = parent;
        }

        /// <summary>
        /// Gets a value from the property store with the specified key of a concrete type
        /// </summary>
        /// <param name="key">Key of the property to get</param>
        /// <param name="defaultValue">Value to return when the specified property is not found in the dictionary</param>
        /// <typeparam name="T">The type of property to get.</typeparam>
        /// <returns>Value of the property with the given key, or <paramref name="defaultValue"/> if not found</returns>
        public T Get<T>(object key)
        {
            object? value;
            return TryGetValue(key, out value) ? (T)value : throw new KeyNotFoundException();
        }

        /// <summary>
        /// Gets a value from the property store with the specified key of a concrete type
        /// </summary>
        /// <param name="key">Key of the property to get</param>
        /// <param name="defaultValue">Value to return when the specified property is not found in the dictionary</param>
        /// <typeparam name="T">The type of property to get.</typeparam>
        /// <returns>Value of the property with the given key, or <paramref name="defaultValue"/> if not found</returns>
        public T Get<T>(object key, Func<T> defaultValue)
        {
            object? value;
            return TryGetValue(key, out value) ? (T)value : defaultValue();
        }

        /// <summary>
        /// Gets a value from the property store with the specified key of a concrete type, and creates a new instance if it doesn't exist yet.
        /// </summary>
        /// <param name="key">Key of the property to get</param>
        /// <typeparam name="T">Type type of property to get.</typeparam>
        /// <returns>Value of the property with the given key, or a new instance if not already added</returns>
        public T? Create<T>(object key)
            where T : new()
        {
            return Create<T>(key, () => new T());
        }

        /// <summary>
        /// Gets a value from the property store with the specified key of a concrete type, and creates a new instance if it doesn't exist yet.
        /// </summary>
        /// <param name="key">Key of the property to get</param>
        /// <param name="create">Delegate to create the object, if it doesn't already exist</param>
        /// <typeparam name="T">Type type of property to get.</typeparam>
        /// <returns>Value of the property with the given key, or a new instance if not already added</returns>
        public T? Create<T>(object key, Func<T> create)
        {
            object? value;
            if (!TryGetValue(key, out value))
            {
                value = create() ?? throw new NullReferenceException();
                TryAdd(key, value);
            }
            return (T) value;
        }

        /// <summary>
        /// Adds a generic event delegate with the specified key
        /// </summary>
        /// <remarks>
        /// This should be called in an event's add accessor.
        /// If you are adding a handler-based event, call <see cref="AddHandlerEvent"/> instead, which will automatically
        /// tell the handler that it needs to be wired up.
        /// 
        /// You can use any subclass of <see cref="System.EventArgs"/> for the type of event handler
        /// 
        /// To trigger the event, use <see cref="TriggerEvent{T}"/>.
        /// </remarks>
        /// <seealso cref="RemoveEvent"/>
        /// <seealso cref="AddHandlerEvent"/>
        /// <example>
        /// Example implementation of a generic event
        /// <code>
        /// 	static readonly object MySomethingEventKey = new object();
        /// 	
        /// 	public event EventHandler&lt;EventArgs&gt; MySomething
        /// 	{
        /// 		add { Properties.AddEvent(MySomethingEvent, value); }
        /// 		remove { Properties.RemoveEvent(MySomethingEvent, value); }
        /// 	}
        /// </code>
        /// </example>
        /// <param name="key">Key of the event to add to</param>
        /// <param name="value">Delegate to add to the event</param>
        public bool AddEvent(object key, Delegate value)
        {
            object? existingDelegate;
            if (TryGetValue(key, out existingDelegate))
            {
                this[key] = Delegate.Combine((Delegate)existingDelegate, value);
                return true;
            }
            else
            {
                return TryAdd(key, value);
            }
        }

        /// <summary>
        /// Adds a handler-based event delegate with the specified key
        /// </summary>
        /// <remarks>
        /// This should be called in an event's add accessor.
        /// This is used for any event that should be triggered by the platform handler. 
        /// This will call <see cref="M:Eto.Widget.IHandler.HandleEvent(string,bool)"/> with the specified <paramref name="key"/> for the
        /// first subscription to the event.
        /// 
        /// You can use any subclass of <see cref="System.EventArgs"/> for the type of event handler
        /// 
        /// To trigger the event, use <see cref="TriggerEvent{T}"/>
        /// </remarks>
        /// <example>
        /// Example implementation of a handler-triggered event
        /// <code>
        /// 	public const string MySomethingEvent = "MyControl.MySomething";
        /// 	
        /// 	public event EventHandler&lt;EventArgs&gt; MySomething
        /// 	{
        /// 		add { Properties.AddHandlerEvent(MySomethingEvent, value); }
        /// 		remove { Properties.RemoveHandlerEvent(MySomethingEvent, value); }
        /// 	}
        /// </code>
        /// </example>
        /// <param name="key">Key of the event to add to</param>
        /// <param name="value">Delegate to add to the event</param>
        public bool AddHandlerEvent(string key, Delegate value)
        {
            return TryAdd(key, value);
        }

        /// <summary>
        /// Removes the event delegate with the specified <paramref name="key"/>
        /// </summary>
        /// <remarks>
        /// Use this in the remove accessor of your event.  See <see cref="AddEvent"/> and <see cref="AddHandlerEvent"/>
        /// for examples.
        /// </remarks>
        /// <param name="key">Key of the event to remove</param>
        /// <param name="value">Delegate to remove from the event</param>
        public bool RemoveEvent(object key, Delegate value)
        {
            object? existingDelegate;
            if (TryGetValue(key, out existingDelegate))
            {
                Delegate.Remove((Delegate)existingDelegate, value);
                return TryRemove(key, out var v); 
          
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Triggers an event with the specified key
        /// </summary>
        /// <remarks>
        /// Call this in your OnMyEvent(EventArgs) method to trigger the event if it has been subscribed to.
        /// This can handle events that have any type of EventArgs.
        /// </remarks>
        /// <example>
        /// This shows how to trigger either a generic event or handler-triggered event:
        /// <code>
        /// 	protected virtual void OnMySomething(EventArgs e)
        /// 	{
        /// 		Properties.TriggerEvent(MySomethingEventKey, this, e);
        /// 	}
        /// </code>
        /// </example>
        /// <param name="key">Key of the generic or handler event</param>
        /// <param name="sender">Object sending the event (usually 'this')</param>
        /// <param name="args">Arguments for the event</param>
        /// <typeparam name="T">Type of the event arguments</typeparam>
        public void TriggerEvent<T>(object key, object sender, T args)
            where T : EventArgs
        {
            object? existingDelegate;
            if (TryGetValue(key, out existingDelegate) && existingDelegate != null)
            {
                ((EventHandler<T>)existingDelegate)(sender, args);
            }
        }

       

       

        /// <summary>
        /// Set the value for the specified property key, raising the <paramref name="propertyChanged"/> handler if it has changed.
        /// </summary>
        /// <remarks>
        /// This is useful when creating properties that need to trigger changed events without having to write boilerplate code.
        /// </remarks>
        /// <example>
        /// <code>
        /// public class MyForm : Form, INotifyPropertyChanged
        /// {
        /// 	static readonly MyPropertyKey = new object();
        /// 
        /// 	public bool MyProperty
        ///		{
        /// 		get { return Properties.Get&lt;bool&gt;(MyPropertyKey); }
        /// 		set { Properties.Set(MyPropertyKey, value, PropertyChanged); }
        /// 	}
        /// 
        /// 	public event PropertyChangedEventHandler PropertyChanged;
        /// }
        /// </code>
        /// </example>
        /// <param name="key">Key of the property to set.</param>
        /// <param name="value">Value for the property.</param>
        /// <param name="defaultValue">Default value of the property to compare when removing the key</param>
        /// <param name="propertyChanged">Property changed event handler to raise if the property value has changed.</param>
        /// <param name="propertyName">Name of the property, or omit to get the property name from the caller.</param>
        /// <typeparam name="T">The type of the property to set.</typeparam>
        /// <returns>true if the property was changed, false if not</returns>
        public void Set<T>(object key, T value, PropertyChangedEventHandler? propertyChanged = null, [CallerMemberName] string? propertyName = null)
        {
            base[key] = value ?? throw new NullReferenceException();
            propertyChanged?.Invoke(Parent, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Set the value for the specified property key, calling the <paramref name="propertyChanged"/> delegate if it has changed.
        /// </summary>
        /// <remarks>
        /// This is useful when creating properties that need to trigger changed events without having to write boilerplate code.
        /// </remarks>
        /// <example>
        /// <code>
        /// public class MyForm : Form
        /// {
        /// 	static readonly MyPropertyKey = new object();
        /// 
        /// 	public bool MyProperty
        ///		{
        /// 		get { return Properties.Get&lt;bool&gt;(MyPropertyKey); }
        /// 		set { Properties.Set(MyPropertyKey, value, OnMyPropertyChanged); }
        /// 	}
        /// 
        /// 	public event EventHandler&lt;EventArgs&gt; MyPropertyChanged;
        /// 	
        ///		protected virtual void MyPropertyChanged(EventArgs e)
        ///		{
        ///			if (MyPropertyChanged != null)
        ///				MyPropertyChanged(this, e);
        ///		}
        /// }
        /// </code>
        /// </example>
        /// <param name="key">Key of the property to set.</param>
        /// <param name="value">Value for the property.</param>
        /// <param name="defaultValue">Default value of the property to compare when removing the key</param>
        /// <param name="propertyChanged">Property changed event handler to raise if the property value has changed.</param>
        /// <typeparam name="T">The type of the property to set.</typeparam>
        /// <returns>true if the property was changed, false if not</returns>
        public void Set<T>(object key, T value, Action propertyChanged)
        {
            
                this.Set<T>(key, value);
                propertyChanged?.Invoke();
                
        }

        class CommandWrapper
        {
            readonly Action<EventHandler<EventArgs>> removeExecute;
            readonly Action<bool> setEnabled;
            readonly Func<object> getArgument;
            public ICommand Command { get; set; }

            public CommandWrapper(ICommand command, Action<bool> setEnabled, Action<EventHandler<EventArgs>> addExecute, Action<EventHandler<EventArgs>> removeExecute, Func<object> getParameter)
            {
                this.Command = command;
                this.setEnabled = setEnabled;
                this.removeExecute = removeExecute;
                this.getArgument = getParameter;
                addExecute(Command_Execute);
                SetEnabled();
                command.CanExecuteChanged += Command_CanExecuteChanged; ;
            }

            public void Unregister()
            {
                removeExecute(Command_Execute);
                Command.CanExecuteChanged -= Command_CanExecuteChanged;
            }

            void Command_Execute(object? sender, EventArgs e)
            {
                Command.Execute(Parameter);
            }

            void Command_CanExecuteChanged(object? sender, EventArgs e)
            {
                SetEnabled();
            }

            object? Parameter
            {
                get { return getArgument != null ? getArgument() : null; }
            }

            public void SetEnabled()
            {
                if (setEnabled != null)
                    setEnabled(Command.CanExecute(Parameter));
            }
        }

        /// <summary>
        /// Sets an <see cref="ICommand"/> value for the specified property <paramref name="key"/>.
        /// </summary>
        /// <param name="key">Key of the property to set</param>
        /// <param name="value">Command instance</param>
        /// <param name="setEnabled">Delegate to set the widget as enabled when the command state changes.</param>
        /// <param name="addExecute">Delegate to attach the execute event handler when the widget invokes the command.</param>
        /// <param name="removeExecute">Delegate to detach the execute event handler.</param>
        /// <param name="getParameter">Delegate to get the parameter to pass to the command</param>
        /// <seealso cref="GetCommand"/>
        public void SetCommand(object key, ICommand value, Action<bool> setEnabled, Action<EventHandler<EventArgs>> addExecute, Action<EventHandler<EventArgs>> removeExecute, Func<object> getParameter)
        {
            var cmd = Get<CommandWrapper>(key);
            if (cmd != null)
            {
                if (ReferenceEquals(cmd.Command, value))
                    return;
                cmd.Unregister();
            }
            Set(key, value != null ? new CommandWrapper(value, setEnabled, addExecute, removeExecute, getParameter) : null);
        }

        /// <summary>
        /// Updates the command's execute status, typically when the CommandParameter changes.
        /// </summary>
        /// <param name="key">Key of the command to execute.</param>
        public void UpdateCommandCanExecute(object key)
        {
            var cmd = Get<CommandWrapper>(key);
            if (cmd != null)
            {
                cmd.SetEnabled();
            }
        }

        /// <summary>
        /// Gets the command instance for the specified property key.
        /// </summary>
        /// <returns>The command instance, or null if it is not set.</returns>
        /// <param name="key">Key of the property to get.</param>
        /// <seealso cref="SetCommand"/>
        public ICommand? GetCommand(object key)
        {
            var cmd = Get<CommandWrapper>(key);
            return cmd != null ? cmd.Command : null;
        }
    }
}