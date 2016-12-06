using System;
using System.Windows.Forms;
using System.IO;
using System.Xml.Serialization;
using System.Reflection;

using TFlex;
using TFlex.Model;
using TFlex.Model.Model2D;
using TFlex.Command;
using TFlex.Drawing;

//Данный файл реализует функциональность приложения.
//Регистрируются команды приложения, иконки команд, пункты меню, панель с кнопками,
//плавающее окно, обработчики событий от документов.

namespace TFlexNF
{
    /// <summary>
	/// Команды приложения в панели и главном меню
	/// </summary>
	enum Commands
    {
        Create = 1, //Команда создания объекта
        Start = 2,
        Out = 3
    };

    /// <summary>
    /// Команды объектов в контекстном меню
    /// </summary>
    enum ObjectCommands
    {
        ObjectContextCommand = 2,
        OutCommand = 3
    };

    /// <summary>
    /// Команды автоменю
    /// </summary>
    enum AutomenuCommands
    {
        Command1
    };

    /// <summary>
    /// ID иконок объектов, генерируемых приложением
    /// </summary>
    enum ObjectTypeIcons
    {
        NewObject
    };


    public delegate void PluginObjectChangedEventHandler(ObjectEventArgs args);

    /// <summary>
	/// Класс приложения. Регистрируем команды, обработчики событий. 
	/// Обрабатываем события, приходящие от различных меню.
	/// </summary>
    public partial class PluginInstance : Plugin
    {
        /// <summary>
		/// Конструктор
		/// </summary>
		/// <param name="factory"></param>
		public PluginInstance(Factory factory) : base(factory)
        {

        }





        public event PluginObjectChangedEventHandler PluginObjectChanged;

        /// <summary>
		/// Подписываемся на удаление объекта и обрабатываем его
		/// </summary>
		/// <param name="args"></param>
		protected override void ObjectDeletedEventHandler(ObjectEventArgs args)
        {
            if (PluginObjectChanged != null) { PluginObjectChanged(args); }
        }

        /// <summary>
        /// Обработчик события, возникающего после изменения объекта
        /// </summary>
        /// <param name="args"></param>
        protected override void ObjectChangedEventHandler(ObjectEventArgs args)
        {
            if (PluginObjectChanged != null) { PluginObjectChanged(args); }
        }

        /// <summary>
        /// Обработчик события, возникающего после создания объекта модели
        /// </summary>
		/// <param name="args"></param>
        protected override void ObjectCreatedEventHandler(ObjectEventArgs args)
        {
            if (PluginObjectChanged != null) { PluginObjectChanged(args); }
        }

        /// <summary>
        /// Загрузка иконок
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        System.Drawing.Icon LoadIconResource(string name)
        {
            System.IO.Stream stream = GetType().Assembly.
                GetManifestResourceStream("TFlexNF" + ".Resource_Files." + name + ".ico");
            return new System.Drawing.Icon(stream);
        }

        /// <summary>
        ///Данная инициализация вызывается в момент загрузки приложения.
        ///В данном приложении здесь ничего делать не нужно. Вся инициализация делается в OnCreateTools
        /// </summary>
        protected override void OnInitialize()
        {
            base.OnInitialize();
            RegisterAutomenuCommand((int)AutomenuCommands.Command1, "Команда", LoadIconResource("IcoCommand"));
        }
        /// <summary>
        /// Этот метод вызывается в тот момент, когда следует зарегистрировать команды,
        /// Создать панель, вставить пункты меню
        /// </summary>
        protected override void OnCreateTools()
        {
            base.OnCreateTools();

            string icoName = "IcoObject";
            RegisterCommand((int)Commands.Create, "$Создание объекта", LoadIconResource(icoName), LoadIconResource(icoName)); // Регистрируем команду создания

            //Регистрируем команды контекстного меню объекта
            RegisterObjectCommand((int)ObjectCommands.ObjectContextCommand, "Собрать геометрию...", LoadIconResource("IcoCommand"), LoadIconResource("IcoCommand")); // Регистрируем команду заливки для контекстного меню
            RegisterObjectCommand((int)ObjectCommands.OutCommand, "Вывести геометрию...", LoadIconResource("IcoCommand"), LoadIconResource("IcoCommand")); // Регистрируем команду заливки для контекстного меню

            //Регистрируем иконку объекта
            RegisterObjectTypeIcon((int)ObjectTypeIcons.NewObject, LoadIconResource(icoName));

            //Добавляем пункты и подпункты меню
            TFlex.Menu submenu = new TFlex.Menu();
            submenu.CreatePopup();
            submenu.Append((int)Commands.Create, "&Создать", this);
            TFlex.Application.ActiveMainWindow.InsertPluginSubMenu("Объекты", submenu, MainWindow.InsertMenuPosition.PluginSamples, this);

            //Создаём панель с кнопками "Звёзды"
            int[] cmdIDs = new int[] { (int)Commands.Create, (int)Commands.Start, (int)Commands.Out };
            CreateToolbar("$Объекты", cmdIDs);
        }



        /// <summary>
        /// Обработка команд от панели и главного меню
        /// </summary>
        /// <param name="document"></param>
        /// <param name="id"></param>
        protected override void OnCommand(Document document, int id)
        {
            switch ((Commands)id)
            {
                default:
                    base.OnCommand(document, id);
                    break;
                case Commands.Create:
                    break;
                case Commands.Start://Команда создания объекта
                    NFGetGeom.Doc = document;
                    
                    NFTask task = NFGetGeom.GetGeometry();
                    NFForm Wndw = new NFForm(task);//Form1(task);
                    Wndw.Activate();
                    Wndw.Show();
                    break;
                case Commands.Out:
                    NFResults.Start();
                    break;
            }
        }

        /// <summary>
        /// Здесь можно блокировать команды и устанавливать галочки
        /// </summary>
        /// <param name="cmdUI"></param>
        protected override void OnUpdateCommand(CommandUI cmdUI)
        {
            if (cmdUI.Document == null)
            {
                cmdUI.Enable(false);
                return;
            }

            cmdUI.Enable();
        }



    }

}