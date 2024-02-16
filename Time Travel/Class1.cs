using System;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using System.Drawing;

public class TimePortalMod : Script
{
    private bool isYearInputActive = false;
    private bool isPortalActive = false;
    private DateTime currentDateTime;
    private Vector3 portalPosition;
    private string yearInput = "";
    private int portalYear;
    private int savedPortalYear;
    private Blip portalBlip;
    private Vector3 savedPlayerPosition;
    private bool isInNorthYankton = false;

    public TimePortalMod()
    {
        Tick += OnTick;
        KeyDown += OnKeyDown;
        Aborted += OnAborted;
    }

    private void OnTick(object sender, EventArgs e)
    {
        if (isYearInputActive)
        {
            int keyboardResult = Function.Call<int>(Hash.UPDATE_ONSCREEN_KEYBOARD);
            if (keyboardResult == 1)
            {
                yearInput = Function.Call<string>(Hash.GET_ONSCREEN_KEYBOARD_RESULT);
                if (int.TryParse(yearInput, out portalYear))
                {
                    if (portalYear < 2004 || portalYear > 2050)
                    {
                        Notification.Show("Ошибка: введите год в диапазоне от 2004 до 2050.");
                        isYearInputActive = false; // Устанавливаем переменную в false в случае ошибки
                        return;
                    }

                    if (portalYear == savedPortalYear)
                    {
                        isYearInputActive = false;
                        Notification.Show("Ошибка: вы уже находитесь в этом году.");
                        return;
                    }

                    savedPortalYear = portalYear;
                    isYearInputActive = false;
                    isPortalActive = true;
                    Notification.Show($"Портал времени создан на {portalYear} год. Пройдите через него, чтобы переместиться во времени.");

                    // Проверяем, играет ли игрок за Майкла и введен ли 2004 год
                    if (Game.Player.Character.Model == PedHash.Michael && portalYear == 2004)
                    {
                        // Загружаем карту Северного Янктона
                        LoadNorthYanktonIPL();

                        // Устанавливаем флаг нахождения в Северном Янктоне
                        isInNorthYankton = true;
                    }
                }
                else
                {
                    Notification.Show("Ошибка: введите корректный год.");
                    isYearInputActive = false; // Устанавливаем переменную в false в случае ошибки
                }
            }
        }

        if (isPortalActive)
        {
            // Отрисовываем маркер портала
            World.DrawMarker(MarkerType.VerticleCircle, portalPosition, Vector3.Zero, Vector3.Zero, new Vector3(5.0f, 5.0f, 5.0f), Color.Yellow);

            // Обработка портала времени
            if (Game.Player.Character.Position.DistanceTo(portalPosition) < 5f)
            {
                if (!isYearInputActive)
                {
                    int year = Function.Call<int>(Hash.GET_CLOCK_YEAR);
                    int month = Function.Call<int>(Hash.GET_CLOCK_MONTH);
                    int day = Function.Call<int>(Hash.GET_CLOCK_DAY_OF_MONTH);

                    // Затемнение экрана
                    GTA.UI.Screen.FadeOut(1000);

                    // Устанавливаем новую дату на указанный год
                    Function.Call(Hash.SET_CLOCK_DATE, portalYear, month, day);

                    // Случайная смена времени суток
                    World.CurrentTimeOfDay = new TimeSpan(0, new Random().Next(0, 24), 0);

                    // Сохраняем текущие координаты игрока перед телепортацией
                    savedPlayerPosition = Game.Player.Character.Position;

                    // Отключаем портал после перемещения во времени
                    isPortalActive = false;

                    // Проверяем, был ли игрок телепортирован в Северный Янктон, и если да, то телепортируем его обратно
                    if (isInNorthYankton && portalYear > 2004)
                    {
                        // Выгружаем карту Северного Янктона
                        UnloadNorthYanktonIPL();

                        // Сбрасываем флаг нахождения в Северном Янктоне
                        isInNorthYankton = false;

                        // Телепортируем игрока обратно на сохраненные координаты
                        Game.Player.Character.Position = savedPlayerPosition;
                    }
                    else
                    {
                        // Проверяем, играет ли игрок за Майкла и введен ли 2004 год
                        if (Game.Player.Character.Model == new Model("player_zero").Hash && portalYear == 2004)
                        {
                            // Телепортируем игрока в Северный Янктон
                            Game.Player.Character.Position = new Vector3(5342.45f, -5189.75f, 82.77225f);

                            // Устанавливаем флаг нахождения в Северном Янктоне
                            isInNorthYankton = true;
                        }
                    }

                    // Осветление экрана
                    GTA.UI.Screen.FadeIn(1000);
                }
            }
        }
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F12)
        {
            if (!isPortalActive && !isYearInputActive)
            {
                // Создание портала времени в 10 метрах от игрока
                portalPosition = Game.Player.Character.Position + Game.Player.Character.ForwardVector * 10f;

                // Создание метки портала на карте
                portalBlip = World.CreateBlip(portalPosition);
                portalBlip.Sprite = BlipSprite.SlowTime;
                portalBlip.Color = BlipColor.Yellow;

                // Запускаем ввод года для портала времени
                isYearInputActive = true;
                Function.Call(Hash.DISPLAY_ONSCREEN_KEYBOARD, 0, "", "", "", "", "", "", 4);
            }
        }
    }

    private void OnAborted(object sender, EventArgs e)
    {
        // Удаление метки портала, если она создана
        if (portalBlip != null && portalBlip.Exists())
        {
            portalBlip.Delete();
        }
    }

    private void LoadNorthYanktonIPL()
    {
        Function.Call(Hash.REQUEST_IPL, "plg_01");
        Function.Call(Hash.REQUEST_IPL, "prologue01");
        Function.Call(Hash.REQUEST_IPL, "prologue01_lod");
        Function.Call(Hash.REQUEST_IPL, "prologue01c");
        Function.Call(Hash.REQUEST_IPL, "prologue01c_lod");
        Function.Call(Hash.REQUEST_IPL, "prologue01d");
        Function.Call(Hash.REQUEST_IPL, "prologue01e");
        Function.Call(Hash.REQUEST_IPL, "prologue01e_lod");
        Function.Call(Hash.REQUEST_IPL, "prologue01f");
        Function.Call(Hash.REQUEST_IPL, "prologue01f_lod");
        Function.Call(Hash.REQUEST_IPL, "prologue01g");
        Function.Call(Hash.REQUEST_IPL, "prologue01h");
        Function.Call(Hash.REQUEST_IPL, "prologue01h_lod");
        Function.Call(Hash.REQUEST_IPL, "prologue01i");
        Function.Call(Hash.REQUEST_IPL, "prologue01i_lod");
        Function.Call(Hash.REQUEST_IPL, "prologue01j");
        Function.Call(Hash.REQUEST_IPL, "prologue01j_lod");
        Function.Call(Hash.REQUEST_IPL, "prologue01k");
        Function.Call(Hash.REQUEST_IPL, "prologue01k_lod");
        Function.Call(Hash.REQUEST_IPL, "prologue01z");
        Function.Call(Hash.REQUEST_IPL, "prologue01z_lod");
        Function.Call(Hash.REQUEST_IPL, "plg_02");
        Function.Call(Hash.REQUEST_IPL, "prologue02");
        Function.Call(Hash.REQUEST_IPL, "prologue02_lod");
        Function.Call(Hash.REQUEST_IPL, "plg_03");
        Function.Call(Hash.REQUEST_IPL, "prologue03");
        Function.Call(Hash.REQUEST_IPL, "prologue03_lod");
        Function.Call(Hash.REQUEST_IPL, "prologue03b");
        Function.Call(Hash.REQUEST_IPL, "prologue03b_lod");
        Function.Call(Hash.REQUEST_IPL, "prologue03_grv_dug");
        Function.Call(Hash.REQUEST_IPL, "prologue03_grv_dug_lod");
        Function.Call(Hash.REQUEST_IPL, "prologue_grv_torch");
        Function.Call(Hash.REQUEST_IPL, "plg_04");
        Function.Call(Hash.REQUEST_IPL, "prologue04");
        Function.Call(Hash.REQUEST_IPL, "prologue04_lod");
        Function.Call(Hash.REQUEST_IPL, "prologue04b");
        Function.Call(Hash.REQUEST_IPL, "prologue04b_lod");
        Function.Call(Hash.REQUEST_IPL, "prologue04_cover");
        Function.Call(Hash.REQUEST_IPL, "des_protree_end");
        Function.Call(Hash.REQUEST_IPL, "des_protree_start_lod");
        Function.Call(Hash.REQUEST_IPL, "plg_05");
        Function.Call(Hash.REQUEST_IPL, "prologue05_lod");
        Function.Call(Hash.REQUEST_IPL, "prologue05b");
        Function.Call(Hash.REQUEST_IPL, "prologue05b_lod");
        Function.Call(Hash.REQUEST_IPL, "plg_06");
        Function.Call(Hash.REQUEST_IPL, "prologue06");
        Function.Call(Hash.REQUEST_IPL, "prologue06_lod");
        Function.Call(Hash.REQUEST_IPL, "prologue06b");
        Function.Call(Hash.REQUEST_IPL, "prologue06b_lod");
        Function.Call(Hash.REQUEST_IPL, "prologue06_int");
        Function.Call(Hash.REQUEST_IPL, "prologue06_int_lod");
        Function.Call(Hash.REQUEST_IPL, "prologue06_pannel");
        Function.Call(Hash.REQUEST_IPL, "prologue06_pannel_lod");
        Function.Call(Hash.REQUEST_IPL, "prologue_m2_door");
        Function.Call(Hash.REQUEST_IPL, "prologue_m2_door_lod");
        Function.Call(Hash.REQUEST_IPL, "plg_occl_00");
        Function.Call(Hash.REQUEST_IPL, "prologue_occl");
        Function.Call(Hash.REQUEST_IPL, "plg_rd");
        Function.Call(Hash.REQUEST_IPL, "prologuerd");
        Function.Call(Hash.REQUEST_IPL, "prologuerdb");
        Function.Call(Hash.REQUEST_IPL, "prologuerd_lod");
    }

    private void UnloadNorthYanktonIPL()
    {
        Function.Call(Hash.REMOVE_IPL, "plg_01");
        Function.Call(Hash.REMOVE_IPL, "prologue01");
        Function.Call(Hash.REMOVE_IPL, "prologue01_lod");
        Function.Call(Hash.REMOVE_IPL, "prologue01c");
        Function.Call(Hash.REMOVE_IPL, "prologue01c_lod");
        Function.Call(Hash.REMOVE_IPL, "prologue01d");
        Function.Call(Hash.REMOVE_IPL, "prologue01e");
        Function.Call(Hash.REMOVE_IPL, "prologue01e_lod");
        Function.Call(Hash.REMOVE_IPL, "prologue01f");
        Function.Call(Hash.REMOVE_IPL, "prologue01f_lod");
        Function.Call(Hash.REMOVE_IPL, "prologue01g");
        Function.Call(Hash.REMOVE_IPL, "prologue01h");
        Function.Call(Hash.REMOVE_IPL, "prologue01h_lod");
        Function.Call(Hash.REMOVE_IPL, "prologue01i");
        Function.Call(Hash.REMOVE_IPL, "prologue01i_lod");
        Function.Call(Hash.REMOVE_IPL, "prologue01j");
        Function.Call(Hash.REMOVE_IPL, "prologue01j_lod");
        Function.Call(Hash.REMOVE_IPL, "prologue01k");
        Function.Call(Hash.REMOVE_IPL, "prologue01k_lod");
        Function.Call(Hash.REMOVE_IPL, "prologue01z");
        Function.Call(Hash.REMOVE_IPL, "prologue01z_lod");
        Function.Call(Hash.REMOVE_IPL, "plg_02");
        Function.Call(Hash.REMOVE_IPL, "prologue02");
        Function.Call(Hash.REMOVE_IPL, "prologue02_lod");
        Function.Call(Hash.REMOVE_IPL, "plg_03");
        Function.Call(Hash.REMOVE_IPL, "prologue03");
        Function.Call(Hash.REMOVE_IPL, "prologue03_lod");
        Function.Call(Hash.REMOVE_IPL, "prologue03b");
        Function.Call(Hash.REMOVE_IPL, "prologue03b_lod");
        Function.Call(Hash.REMOVE_IPL, "prologue03_grv_dug");
        Function.Call(Hash.REMOVE_IPL, "prologue03_grv_dug_lod");
        Function.Call(Hash.REMOVE_IPL, "prologue_grv_torch");
        Function.Call(Hash.REMOVE_IPL, "plg_04");
        Function.Call(Hash.REMOVE_IPL, "prologue04");
        Function.Call(Hash.REMOVE_IPL, "prologue04_lod");
        Function.Call(Hash.REMOVE_IPL, "prologue04b");
        Function.Call(Hash.REMOVE_IPL, "prologue04b_lod");
        Function.Call(Hash.REMOVE_IPL, "prologue04_cover");
        Function.Call(Hash.REMOVE_IPL, "des_protree_end");
        Function.Call(Hash.REMOVE_IPL, "des_protree_start_lod");
        Function.Call(Hash.REMOVE_IPL, "plg_05");
        Function.Call(Hash.REMOVE_IPL, "prologue05_lod");
        Function.Call(Hash.REMOVE_IPL, "prologue05b");
        Function.Call(Hash.REMOVE_IPL, "prologue05b_lod");
        Function.Call(Hash.REMOVE_IPL, "plg_06");
        Function.Call(Hash.REMOVE_IPL, "prologue06");
        Function.Call(Hash.REMOVE_IPL, "prologue06_lod");
        Function.Call(Hash.REMOVE_IPL, "prologue06b");
        Function.Call(Hash.REMOVE_IPL, "prologue06b_lod");
        Function.Call(Hash.REMOVE_IPL, "prologue06_int");
        Function.Call(Hash.REMOVE_IPL, "prologue06_int_lod");
        Function.Call(Hash.REMOVE_IPL, "prologue06_pannel");
        Function.Call(Hash.REMOVE_IPL, "prologue06_pannel_lod");
        Function.Call(Hash.REMOVE_IPL, "prologue_m2_door");
        Function.Call(Hash.REMOVE_IPL, "prologue_m2_door_lod");
        Function.Call(Hash.REMOVE_IPL, "plg_occl_00");
        Function.Call(Hash.REMOVE_IPL, "prologue_occl");
        Function.Call(Hash.REMOVE_IPL, "plg_rd");
        Function.Call(Hash.REMOVE_IPL, "prologuerd");
        Function.Call(Hash.REMOVE_IPL, "prologuerdb");
        Function.Call(Hash.REMOVE_IPL, "prologuerd_lod");
    }
}