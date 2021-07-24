using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using DxLibDLL;
using System.Drawing;
using System.IO;

namespace BombSort
{

    static class ProgramProperty
    {
        public const string version = "1.0.0";
        public const string published = "2021/07/03";
        public const string maker = "sashi";
    }



    //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
    static class MainGame
    {

        const int DRAW_WIDTH = 416;
        const int DRAW_HEIGHT = 240;

        static Random rand = new Random();


        const uint MASK_BAKUGON = 0b1;
        const int STATE_FREE = 0, STATE_HAND = 1, STATE_CAUGHT = 2;

        const int POP_UP = 0, POP_DN = 1;
        const int TYPE_W = 0, TYPE_R = 1;




        [STAThread]
        static void Main()
        {
            const float draw_scale = 3;
            const int realm_width = 16*7;


            // ウインドウモードで起動
            DX.ChangeWindowMode(DX.TRUE);
            DX.SetGraphMode((int)(DRAW_WIDTH * draw_scale), (int)(DRAW_HEIGHT * draw_scale), 16);
            

            DX.SetAlwaysRunFlag(1); //非アクティブ状態でも動かす



            // ＤＸライブラリの初期化
            if (DX.DxLib_Init() < 0)
            {
                return;
            }
            Sprite.Init(); // スプライトの初期化


            DX.SetMainWindowText("爆弾どの爆弾");



            //int Hndl_ = DX.LoadGraph(@"Assets\Images\.png");
            int Hndl_Map = DX.LoadGraph(@"Assets\Images\マップ02加工01.png");
            int Hndl_hand = DX.LoadGraph(@"Assets\Images\つかみ2f16.png");
            int Hndl_BakugonR = DX.LoadGraph(@"Assets\Images\赤バクゴン4f32.png");
            int Hndl_BakugonW = DX.LoadGraph(@"Assets\Images\白バクゴン4f32.png");
            int Hndl_inkP = DX.LoadGraph(@"Assets\Images\紫インク4f48.png");
            int Hndl_inkY = DX.LoadGraph(@"Assets\Images\黄インク4f48.png");
            int Hndl_BakugonEffect = DX.LoadGraph(@"Assets\Images\バクゴンエフェクト4f32.png");
            int Hndl_star1 = DX.LoadGraph(@"Assets\Images\青星4f24.png");
            int Hndl_star2 = DX.LoadGraph(@"Assets\Images\橙星4f24.png");
            int Hndl_batibati = DX.LoadGraph(@"Assets\Images\バクゴン火花4f32.png");
            int Hndl_smoke = DX.LoadGraph(@"Assets\Images\煙4f32.png");

            int Hndl_greenBack = DX.LoadGraph(@"Assets\Images\緑背景416_240.png");
            int Hndl_titleFront = DX.LoadGraph(@"Assets\Images\爆弾どの爆弾タイトル.png");


            //int se_ = DX.LoadSoundMem(@"Assets\Sounds\.mp3");
            //int se_ok = DX.LoadSoundMem(@"Assets\Sounds\決定、ボタン押下22.mp3");
            int se_sceneChange = DX.LoadSoundMem(@"Assets\Sounds\シーン切り替え1.mp3");
            int se_nyu = DX.LoadSoundMem(@"Assets\Sounds\ニュッ3.mp3");
            //int se_puyon = DX.LoadSoundMem(@"Assets\Sounds\ぷよん.mp3");
            int se_paku = DX.LoadSoundMem(@"Assets\Sounds\食べ物をパクッ.mp3");
            int se_bakuhatsu = DX.LoadSoundMem(@"Assets\Sounds\爆発2.mp3");
            int se_ashioto = DX.LoadSoundMem(@"Assets\Sounds\可愛い足音.mp3");
            int se_bubu = DX.LoadSoundMem(@"Assets\Sounds\クイズ不正解1.mp3");

            int bgm_haru = DX.LoadSoundMem(@"Assets\Sounds\harunoyokan.mp3");

            //int Hndl_font32; CreateFontToHandle(char * FontName, int Size, int Thick, int FontType);

            List<int> score_top = new List<int>(0);
            List<int> score_last = new List<int>(0);
            for (int i=0; i<10; i++)
            {
                score_top.Add(0);
                score_last.Add( 0);
            }
            int cur_top = -1, cur_last = -1;

            sav_load();

            DX.PlaySoundMem(bgm_haru, DX.DX_PLAYTYPE_LOOP);
            DX.ChangeVolumeSoundMem(128, bgm_haru);
            // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            GameLoop:
            // --------------------------------------



            int Hndl_BackScreen = DX.MakeScreen(DRAW_WIDTH, DRAW_HEIGHT, 0);

            int Hndl_DrawScreen = DX.MakeScreen(DRAW_WIDTH, DRAW_HEIGHT, 0);





            int player_sp, player_x=0, player_y=0;
            int player_catch = -1;

            int bakugon_pop = 0;
            int bakugon_Init = 0;
            List<int> bakugon_sp = new List<int>(0);


            int game_score = 0;
            int game_state = 0;
            int finish_count = 0;
            const int GAME_PLAYING = 0, GAME_OVER = 1;

            bool game_continue = false;

            int loop_cnt = 0;





            Title_Loop();

            Main_Loop();


            if (game_continue) goto GameLoop;




            // ＤＸライブラリの後始末
            DX.DxLib_End();
            Sprite.End(); //スプライトの後始末
            return;
            // ===================================================================== プログラム終了




            // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ メインループ
            void Main_Loop()
            {
                DX.SetMouseDispFlag(DX.FALSE); //マウスカーソルの非表示
                Player_Start();
                Map_Set();
                Bakugon_Init();

                Input.Start();
                while (DX.ProcessMessage() != -1)
                {
                    Input.Update();

                    if (DX.CheckHitKey(DX.KEY_INPUT_ESCAPE) == 1) //escape押されたら終了
                    {
                        break;
                    }

                    if (game_state == GAME_OVER) //ゲームオーバー
                    {
                        finish_count++;

                        if (finish_count == 240)
                        {
                            toTitle_effect();
                        }

                        if (finish_count > 300)
                        {
                            ScoreList_sort();
                            sav_save();
                            game_continue = true;
                            Sprite.Clear();
                            break;
                        }
                    }




                    Bakugon_Pop();

                    Player_Update();

                    Sprite.AllUpdate();

                    Screen_Update();
                    loop_cnt++;
                }

            }// --------------------------------------------------------- メインループ

            //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^タイトルループ
            void Title_Loop()
            {
                DX.SetMouseDispFlag(DX.TRUE); //マウスカーソルの非表示
                Title_Start();

                Input.Start();
                bool f = false;
                while (DX.ProcessMessage() != -1)
                {
                    Input.Update();

                    if (DX.CheckHitKey(DX.KEY_INPUT_ESCAPE) == 1) //escape押されたら終了
                    {
                        break;
                    }

                    if (Input.ClickLeft_time == 1)
                    {
                        f = true;
                        DX.PlaySoundMem(se_sceneChange, DX.DX_PLAYTYPE_BACK);
                        break;
                    }

                    Title_Update();

                    Screen_Project();

                    DetailGraphics_Update();

                    DX.ScreenFlip();
                }
                //Sprite.Clear();

                if (f) Useful.Wait(30);


                return;
                //================== 終了


                //開始処理
                void Title_Start()
                {
                    DX.SetDrawScreen(Hndl_BackScreen);
                    DX.DrawGraph(0,0,Hndl_greenBack,1);
                    DX.DrawGraph(0, 0, Hndl_titleFront, 1);
                    
                    Useful.DrawString_bordered(0, 0, "ver" + ProgramProperty.version,DX.GetColor(255,255,255), DX.GetColor(100,30,30));
                    Useful.DrawString_bordered(0, 240-16, "a game by " + ProgramProperty.maker, DX.GetColor(255, 255, 255), DX.GetColor(100, 30, 30));
                    Useful.DrawString_bordered(160, 200, "左クリックでスタート" , DX.GetColor(255, 255, 255), DX.GetColor(100, 30, 30));


                    for (int x = 360; x < 408; x++)
                    {
                        DX.SetDrawBlendMode(DX.DX_BLENDMODE_ALPHA, (x-360)*2);
                        DX.DrawFillBox(x, 4, x+1, 232, DX.GetColor(255, 200, 200));
                    }
                    DX.SetDrawBlendMode(DX.DX_BLENDMODE_NOBLEND,0);
                }

                //更新処理
                void Title_Update()
                {


                    DX.SetDrawScreen(Hndl_DrawScreen);
                    DX.DrawGraph(0, 0, Hndl_BackScreen, DX.TRUE);


                }


                void DetailGraphics_Update()
                {
                    DX.SetFontSize((int)(16*draw_scale/2));
                    DX.ChangeFontType(DX.DX_FONTTYPE_EDGE);

                    int y1 = 8;
                    bool F = false;

                    string s1 = "Top 10";
                    DX.DrawString(ToDraw(400) - DX.GetDrawStringWidth(s1, s1.Length * 8), ToDraw(nextY()), s1,DX.GetColor(255,255,255),DX.GetColor(100,30,30));
                    y1 += 8;
                    for (int y=0; y<10; y++)
                    {
                        string s = score_top[y].ToString();
                        if (cur_top == y) s += " <";
                        DX.DrawString(ToDraw(400) - DX.GetDrawStringWidth(s, s.Length * 8), ToDraw(nextY()), s, DX.GetColor(255, 255, 255), DX.GetColor(100, 30, 30));
                    }

                    y1 += 8 * 2;

                    s1 = "Last 10";
                    DX.DrawString(ToDraw(400) - DX.GetDrawStringWidth(s1, s1.Length * 8), ToDraw(nextY()), s1, DX.GetColor(255, 255, 255), DX.GetColor(100, 30, 30));
                    y1 += 8;
                    for (int y = 0; y < 10; y++)
                    {
                        string s = score_last[y].ToString();
                        if (cur_last == y) s += " <";
                        DX.DrawString(ToDraw(400) - DX.GetDrawStringWidth(s, s.Length * 8), ToDraw(nextY()), s, DX.GetColor(255, 255, 255), DX.GetColor(100, 30, 30));
                    }



                    //DX.DrawString(ToDraw(100), ToDraw(0), "Last score", DX.GetColor(255, 255, 255), DX.GetColor(100, 30, 30));
                    DX.SetFontSize(16);


                    int ToDraw(double x)
                    {
                        return (int)(x * draw_scale);
                    }


                    int nextY()
                    {
                        int y = y1; y1 += 8;
                        return y;
                    }

                }








            }
            //-----------------------------------------------------------

            void ScoreList_sort()
            {
                score_last.Insert(0, game_score);
                
                score_top.Add(game_score);
                score_top.Sort();
                score_top.Reverse();//降順に

                score_last.RemoveAt(10);
                score_top.RemoveAt(10);

                //スコアカーソルの計算
                cur_last = 0;
                for (int i=0; i<10; i++)
                {
                    if (score_top[i] == game_score)
                    {
                        cur_top = i; break;
                    }
                }
            }




            void sav_save()
            {
                string path = "data.sav";
                byte[] binary = new byte[2 * 10 *2];

                for (int i = 0; i <= 9; i++)
                {
                    binary[i * 2 + 0] = (byte)(score_last[i] / 0xFF);
                    binary[i * 2 + 1] = (byte)(score_last[i] % 0xFF);
                }
                for (int i = 0; i <= 9; i++)
                {
                    binary[20 + i * 2 + 0] = (byte)(score_top[i] / 0xFF);
                    binary[20 + i * 2 + 1] = (byte)(score_top[i] % 0xFF);
                }

                File.WriteAllBytes(path, binary);
            }
            //--------------------------------
            void sav_load()
            {
                string path = "data.sav";

                if (!File.Exists(path)) return;

                byte[] binary = File.ReadAllBytes(path);

                for (int i = 0; i <= 9; i++)
                {
                    score_last[i] = binary[i * 2 + 0] * 0xFF + binary[i * 2 + 1];
                }
                for (int i = 0; i <= 9; i++)
                {
                    score_top[i] = binary[20+i * 2 + 0] * 0xFF + binary[20+i * 2 + 1];
                }

                File.WriteAllBytes(path, binary);
            }





            //-----------------------------------------------------------


            //マップを表示
            void Map_Set()
            {
                DX.SetDrawScreen(Hndl_BackScreen);
                DX.DrawGraph(0,0,Hndl_Map, DX.TRUE);
            }







            //プレイヤー関連
            void Player_Start()
            {
                player_sp = Sprite.Set(Hndl_hand, 0, 0, 16, 16);
            }

            void Player_Update(){
                DX.GetMousePoint(out player_x, out player_y);
                player_x = (int)(player_x / draw_scale);
                player_y = (int)(player_y / draw_scale);

                if (game_state==GAME_PLAYING && Input.ClickLeft_time==1) //つかむ
                {
                    int h = Useful.Sprite_HitRectangle_front(player_x, player_y, 16, 16, MASK_BAKUGON);
                    if (h>-1)
                    {
                        Properties.Bakugon p = (Properties.Bakugon)Sprite.sprite[h].Dict["prop"];
                        if (p.State == STATE_FREE)
                        {
                            player_catch = h;
                            Sprite.Image(player_sp, 16, 0, 16, 16);
                            DX.PlaySoundMem(se_paku, DX.DX_PLAYTYPE_BACK);
                        }
                    }
                }

                if (game_state == GAME_OVER || Input.ClickLeft_time == 0)//何もつかんでない
                {
                    player_catch = -1;
                    Sprite.Image(player_sp, 0, 0, 16, 16);
                }





                Sprite.Offset(player_sp, player_x, player_y,-1000);
                //Console.WriteLine($"{player_x},{player_y}");


            }







            //バクゴン関連

            void Bakugon_Init()
            {
                bakugon_Init = 2; Bakugon_Set(0);
                bakugon_Init = 1; Bakugon_Set(0);
                bakugon_Init = 0;
            }



            //ポップ
            void Bakugon_Pop()
            {
                //if (((loop_cnt % 90) == 0 && bakugon_pop < 20) || ((loop_cnt % 60) == 0 && bakugon_pop >= 20))
                if ((loop_cnt % 120) == 0 && Useful.between(bakugon_pop, 0, 179))
                {
                    DX.PlaySoundMem(se_ashioto, DX.DX_PLAYTYPE_BACK);
                    if (bakugon_pop < 4)
                    {
                        Bakugon_Set(POP_DN);
                    }
                    else if (bakugon_pop < 10)
                    {
                        Bakugon_Set(rand.Next(0, 2));
                    }
                    else if (bakugon_pop < 40)
                    {
                        int a = rand.Next(0, 2);
                        Bakugon_Set(a); Bakugon_Set(a);
                    }
                    else if (bakugon_pop < 60)
                    {
                        Bakugon_Set(0); Bakugon_Set(1);
                        if (rand.Next(0,3)==0) Bakugon_Set(rand.Next(0, 2));
                    }
                    else if (bakugon_pop < 100)
                    {
                        if (rand.Next(0, 2) == 0)
                        {
                            int a = rand.Next(0, 2);
                            Bakugon_Set(a); Bakugon_Set(a);
                        }
                        else
                        {
                            Bakugon_Set(0); Bakugon_Set(1);
                        }
                        if (75 < bakugon_pop && rand.Next(0, 5) == 0)
                        {
                            Bakugon_Set(rand.Next(0, 2));
                        }
                    }
                    else if (bakugon_pop < 150)
                    {
                        int a = rand.Next(0, 2);
                        if (bakugon_pop < 120 && rand.Next(0, 2) == 0)
                        {
                            for (int i=0;i<rand.Next(0,2);i++) Bakugon_Set(a);
                        }
                        else
                        {
                            for (int i = 0; i <= 2+rand.Next(1, 3); i++)
                            {
                                Bakugon_Set(a);
                            }
                        }
                    }
                    else if (bakugon_pop < 180)
                    {
                        Bakugon_Set(rand.Next(0, 2)); Bakugon_Set(rand.Next(0, 2)); Bakugon_Set(rand.Next(0, 2));
                    }
                }
                /*
                if ((loop_cnt % 60) == 0)
                {
                    if (bakugon_pop >= 70)
                    {
                        Bakugon_Set(rand.Next(0, 2));
                    }
                }
                */
                if ((loop_cnt % 60) == 0 && Useful.between(bakugon_pop, 180,199))
                {
                    Bakugon_Set(rand.Next(0, 2));
                    if (rand.Next(0,3)==0) Bakugon_Set(rand.Next(0, 2));
                    DX.PlaySoundMem(se_ashioto, DX.DX_PLAYTYPE_BACK);
                }
                if ((loop_cnt % 20) == 0 && bakugon_pop>=200)
                {
                    Bakugon_Set(rand.Next(0, 2));
                    if (bakugon_pop>=220)
                    {
                        for (int i=0; i<rand.Next(1,3);i++) Bakugon_Set(rand.Next(0, 2));
                    }
                    DX.PlaySoundMem(se_ashioto, DX.DX_PLAYTYPE_BACK);
                }

            }


            void Bakugon_Set(int aus)
            {

                int image = (rand.Next(0, 2) == 0) ? Hndl_BakugonW : Hndl_BakugonR;

                if (bakugon_Init == 2) image = Hndl_BakugonR;
                if (bakugon_Init == 1) image = Hndl_BakugonW;

                int sp = Sprite.Set(image, 0, 0, 32, 32);

                Sprite.sprite[sp].mask = MASK_BAKUGON;

                Properties.Bakugon prop = new Properties.Bakugon();
                Sprite.sprite[sp].Dict.Add("prop",  prop);

                prop.image = image;
                prop.x = (float)DRAW_WIDTH / 2 - 16;
                prop.y = ((aus == POP_UP) ? -32 : DRAW_HEIGHT + 16);
                prop.ang = 0f;
                prop.d_ang = 0.2f;
                prop.speed = 2f;
                prop.d_ang = (rand.Next(0, 2)==0) ? 1 : -1;
                prop.link = Sprite.Set(Hndl_batibati, 0, 0, 32, 32);
                prop.type = (image == Hndl_BakugonW) ? TYPE_W : TYPE_R;



                Sprite.Offset(sp, prop.x, prop.y);
                Sprite.Offset(prop.link, prop.x, prop.y,-500);
                Sprite.sprite[sp].Update += Bakugon_Update;

                bakugon_sp.Add(sp);

                if (bakugon_Init == 0) //通常ではポップカウント増加
                {
                    bakugon_pop++;
                }
                else//初期化用
                {
                    if (bakugon_Init == 2)
                    {
                        prop.x = 0;
                    }
                    else
                    {
                        prop.x = DRAW_WIDTH - 32;
                    }
                    prop.y = DRAW_HEIGHT / 2 - 32;
                    prop.State = STATE_CAUGHT;
                    prop.link = -1;
                }

            }


            void Bakugon_Update(int sp)
            {
                Properties.Bakugon prop = (Properties.Bakugon)Sprite.sprite[sp].Dict["prop"];

                float x0 = prop.x, y0 = prop.y;
                float ang = prop.ang, speed = prop.speed;
                
                float vx, vy;



                if (player_catch == sp) //手に捕まった
                {
                    prop.State = STATE_HAND;
                }
                else if (prop.State == STATE_HAND) //放された
                {
                    DX.PlaySoundMem(se_nyu, DX.DX_PLAYTYPE_BACK);

                    if (prop.x < realm_width - 32 || DRAW_WIDTH - realm_width < prop.x)//とらわれた
                    {
                        Star_Effect((int)prop.x+4, (int)prop.y+4);

                        Sprite.Clear(prop.link);
                        prop.link = -1;

                        if (game_score > 255)//捕まえすぎなら表示しない
                        {
                            Sprite.Clear(sp);
                            bakugon_sp.Remove(sp);
                            return;
                        }




                        if (prop.x < realm_width)//左の領域
                        {
                            if (prop.type==TYPE_W)//間違えた
                            {
                                GameMissed_effect(TYPE_R);
                                return;
                            }
                        }
                        else//右の領域
                        {
                            if (prop.type == TYPE_R)//間違えた
                            {
                                GameMissed_effect(TYPE_W);
                                return;
                            }
                        }

                        prop.State = STATE_CAUGHT;
                        Score_plus();
                    }
                    else //とらわれてない
                    {
                        prop.State = STATE_FREE;
                    }
                }







                if (prop.State == STATE_FREE || prop.State == STATE_CAUGHT) //動き回る
                {
                    if (rand.Next(0, 121) == 0) //偏角変化量変更
                    {
                        prop.d_ang = (1 - rand.Next(0, 2) * 2) * (1 + rand.Next(0, 3));
                    }
                    if (rand.Next(0, 121) == 0) //偏角変更
                    {
                        prop.ang += (rand.Next(1, 11)) * 30;
                    }
                    if (rand.Next(0, 121) == 0) //移動半径変更
                    {
                        prop.radius = rand.Next(1, 4) / 2f;
                    }

                    prop.ang += prop.d_ang;

                    //画面外に行かないように
                    if (prop.State == STATE_FREE)
                    {
                        if (prop.x < realm_width) prop.ang = 0f;
                        if (prop.x > DRAW_WIDTH - 32 - realm_width) prop.ang = 180f;
                        if (prop.y < 0) prop.ang = 90f;
                        if (prop.y > DRAW_HEIGHT - 32) prop.ang = -90f;

                        if (prop.count % 6 == 0) Bakugon_Ink(prop.type, (int)prop.x, (int)prop.y); //インクまき散らす
                    }
                    else//捕まってるとき
                    {
                        if (prop.x< realm_width)//左の領域
                        {
                            if (prop.x < 0) prop.ang = 0f;
                            if (prop.x > realm_width - 32) prop.ang = 180f;
                        }
                        else//右の領域
                        {
                            if (prop.x < DRAW_WIDTH - realm_width) prop.ang = 0f;
                            if (prop.x > DRAW_WIDTH - 32) prop.ang = 180f;
                        }
                        if (prop.y < 16) prop.ang = 90f;
                        if (prop.y > DRAW_HEIGHT - 32 -16) prop.ang = -90f;

                    }



                    vx = (float)Math.Cos(Math.PI * prop.ang / 180) * prop.radius;
                    vy = (float)Math.Sin(Math.PI * prop.ang / 180) * prop.radius;


                    prop.x += vx;
                    prop.y += vy;

                }


                if (prop.State == STATE_HAND)
                {
                    prop.x = player_x - 8;
                    prop.y = player_y - 8;
                }





                //Console.WriteLine($"{prop.x},{prop.y}");
                short z = (short)-prop.y;
                if (prop.State == STATE_HAND) z = -500;
                Sprite.Offset(sp, prop.x, prop.y, z);

                Sprite.Image(sp, prop.image, ((prop.count%60)/15)*32, 0, 32, 32);

                if (prop.link > -1)
                {
                    Sprite.Offset(prop.link, prop.x, prop.y, -500);

                    Sprite.Image(prop.link, ((prop.count % 40) / 10) * 32, 0, 32, 32);
                    if (prop.count > 300 && game_state == GAME_PLAYING)//危ない
                    {
                        if (prop.count%10<4) Sprite.Image(sp, Hndl_BakugonEffect, ((prop.count % 60) / 15) * 32, 0, 32, 32);
                        Sprite.Image(prop.link, ((prop.count % 40) / 10) * 32, 32, 32, 32);
                    }
                    if (prop.count > 480 && game_state == GAME_PLAYING)
                    {
                        GameMissed_effect(-1);
                        return;
                    }
                }

                prop.count += 1;
            }

            //インクまき散らす
            void Bakugon_Ink(int type, int x0, int y0)
            {
                x0 -= 8;y0 = y0 - 8 + 16;
                int image = (type == TYPE_R) ? Hndl_inkP : Hndl_inkY;

                int f = rand.Next(0, 4);

                DX.SetDrawScreen(Hndl_BackScreen);
                DX.DrawRectGraph(x0,y0, f*48,0,48,48,image,1);
            }





            void Score_plus()
            {
                game_score++;
                /*
                if (game_score < 64)
                {
                    game_score += 3;
                }
                else if (game_score < 512)
                {
                    game_score += 7;
                }
                else
                {
                    game_score += 15;
                }
                */

            }



            void Star_Effect(int x0, int y0)
            {
                for (int a = 0; a < 12; a++)
                {
                    int sp = Sprite.Set((a%2==0) ? Hndl_star1 : Hndl_star2, 0, 0, 24, 24);

                    int x1 = x0 + 0, y1 = y0 + 0;
                    int x2 = x1 + (int)(Math.Cos(Math.PI * (a * 30f / 180f)) * 128);
                    int y2 = y1 + (int)(Math.Sin(Math.PI * (a * 30f / 180f)) * 128);

                    Sprite.Offset(sp, x1, y1, -500);

                    Sprite.Anim(sp, Sprite.AnimType_XY
                        , -20, x2, y2
                        , 1);
                    Sprite.Anim(sp, Sprite.AnimType_UV
                        , 5, 24 * 0, 0
                        , 5, 24 * 1, 0
                        , 5, 24 * 2, 0
                        , 5, 24 * 3, 0
                        , 0
                        );

                    Sprite.sprite[sp].Update += new SpriteCompornent.UpdateDelegate(Useful.Sprite_EffeectfadeXY);
                }
            }



            //^^^^^^^^^^^^^^^^^^^^^^^^^^^^ ミスエフェクト
            void GameMissed_effect(int type)
            {

                if (game_state == GAME_OVER) return;
                game_state = GAME_OVER;


                //ぶっぶー
                DX.PlaySoundMem(se_bubu, DX.DX_PLAYTYPE_BACK);
                Useful.Wait(60);
                DX.PlaySoundMem(se_bakuhatsu, DX.DX_PLAYTYPE_BACK);


                switch (type)
                {
                    case -1://時間切れ
                        {
                            for (int i=0; i<bakugon_sp.Count() ; i++)
                            {
                                Properties.Bakugon prop = (Properties.Bakugon)Sprite.sprite[bakugon_sp[i] ].Dict["prop"];
                                if (prop.State == STATE_FREE || prop.State == STATE_HAND)
                                {
                                    smoke_effect((int)prop.x, (int)prop.y);
                                    Sprite.Clear(bakugon_sp[i]);
                                }
                            }
                            break;
                        }
                    case TYPE_R://赤
                        {
                            for (int i = 0; i < bakugon_sp.Count(); i++)
                            {
                                Properties.Bakugon prop = (Properties.Bakugon)Sprite.sprite[bakugon_sp[i] ].Dict["prop"];
                                if ((prop.State == STATE_CAUGHT && prop.type == TYPE_R) || prop.State == STATE_HAND)
                                {
                                    smoke_effect((int)prop.x, (int)prop.y);
                                    Sprite.Clear(bakugon_sp[i]);
                                    game_score--;
                                }
                            }
                            game_score += 2;
                            break;
                        }
                    case TYPE_W://白
                        {
                            for (int i = 0; i < bakugon_sp.Count(); i++)
                            {
                                Properties.Bakugon prop = (Properties.Bakugon)Sprite.sprite[bakugon_sp[i]].Dict["prop"];
                                if ((prop.State == STATE_CAUGHT && prop.type == TYPE_W) || prop.State == STATE_HAND)
                                {
                                    smoke_effect((int)prop.x, (int)prop.y);
                                    Sprite.Clear(bakugon_sp[i]);
                                    game_score--;
                                }
                            }
                            game_score += 2;
                            break;
                        }


                }
            }


            void smoke_effect(int x0, int y0)
            {
                for (int i=0; i<4; i++)
                {
                    int x1 = x0 - 16 + (i / 2) * 32;
                    int y1 = y0 - 16 + (i % 2) * 32;

                    int sp = Sprite.Set(Hndl_smoke, 0,0,32,32);
                    Sprite.Offset(sp, x1, y1, -800);

                    Sprite.Anim(sp, Sprite.AnimType_UV,
                        15, 0, 0,
                        15, 32, 0,
                        5, 32 * 2, 0,
                        5, 32 * 3, 0
                        );

                    Sprite.sprite[sp].Update += new SpriteCompornent.UpdateDelegate(Useful.Sprite_Effeectfade); //削除処理追加
                }
            }


            //タイトルへ戻るときのエフェクト
            void toTitle_effect()
            {
                int sp1 = Sprite.Set(Hndl_greenBack,0,0,416,240);
                int sp2 = Sprite.Set(Hndl_titleFront, 0, 0, 416, 240);

                Sprite.Offset(sp1, 0, -240, -4000);
                Sprite.Offset(sp2, 0, 240, -4000);

                Sprite.Anim(sp1, Sprite.AnimType_XY,
                    -60, 0, 0);
                Sprite.Anim(sp2, Sprite.AnimType_XY,
                    -60, 0, 0);

            }






            //UI更新
            void UI_Update()
            {
                Useful.DrawString_bordered(0,0,$"捕獲: {game_score}匹");

                if (game_state == GAME_OVER)
                {
                    Useful.DrawString_bordered(128, DRAW_HEIGHT/2, "G A M E  O V E R", DX.GetColor(255,120,120));
                }

            }











            void Screen_Update()
            {
                // 背景塗りつぶし

                DX.SetDrawScreen(Hndl_DrawScreen);
                DX.DrawGraph(0, 0, Hndl_BackScreen, DX.TRUE);


                // マップを描画


                //スプライト描画
                Sprite.Drawing();

                //UI描画
                UI_Update();
                
                Screen_Project();

                DX.ScreenFlip(); // 裏画面の内容を表画面に反映する
            }


            //画面を拡大して表示
            void Screen_Project()
            {
                DX.SetDrawScreen(DX.DX_SCREEN_BACK);
                DX.DrawExtendGraph(0, 0, (int)(DRAW_WIDTH * draw_scale), (int)(DRAW_HEIGHT * draw_scale), Hndl_DrawScreen, DX.FALSE);

            }


        }





        //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ Properties
        public class Properties
        {

            public class Bakugon
            {
                public int image;
                public float x, y;
                public float ang, radius;
                public float d_ang;
                public float speed;
                public int count;
                public int State = 0;
                public int link;
                public int type = 0;

                public Bakugon()
                {
                    count = 0;
                    radius = 1;
                    State = STATE_FREE;
                }
            }
        }
        //------------------------------------------------------------------- Properties







    }//------------------------------------------------------------------------------------- MainGame 







































































    //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ class Sprote
    class SpriteCompornent
    {
        public bool used;
        public float X, Y;
        //public float Z;
        public int image;
        public int U, V;
        public int Width, Height;
        public int Attribution;
        public uint mask;

        public delegate void UpdateDelegate(int sp);
        public UpdateDelegate Update;

        //アニメ関連
        public static readonly int ANIM_MAX = 8;
        public List<long>[] AnimData;
        public bool[] AnimFrag;
        public bool[] AnimRelative;
        public int[] AnimStep;
        public int[] AnimCount;
        public bool[] AnimDelete;


        //内部変数
        public Dictionary<string, object> Dict;



        public SpriteCompornent()
        {
            used = false;
            X = 0;
            Y = 0;
            //Z = 0;
            image = -1;
            U = 0;
            V = 0;
            Width = 0;
            Height = 0;
            Attribution = 0;
            mask = 0;

            AnimData = new List<long>[ANIM_MAX + 1]; //現状, アニメデータは最大8個まで追加予定
            AnimFrag = new bool[ANIM_MAX + 1];
            AnimRelative = new bool[ANIM_MAX + 1];
            AnimStep = new int[ANIM_MAX + 1];
            AnimCount = new int[ANIM_MAX + 1];
            AnimDelete = new bool[ANIM_MAX + 1];

            Dict = new Dictionary<string, object>();

            Update = delegate (int sp) { }; //デリゲート
        }

    }




    static class Sprite
    {
        public static readonly int SPRITE_MAX = 4096;

        public static SpriteCompornent[] sprite = new SpriteCompornent[SPRITE_MAX];
        public static Dictionary<int, short> sprite_Z = new Dictionary<int, short>();

        public static int NextNum = 0;

        public static int Attribution_reverse = 0b1;

        public static bool WasDisposed;

        public static int ThreadFPS = 60;


        //Spriteクラス初期化
        public static void Init()
        {
            for (int i = 0; i < SPRITE_MAX; i++)
            {
                sprite[i] = new SpriteCompornent();
                sprite_Z.Add(i, 0);
            }

            WasDisposed = false;

            Thread thread1 = new Thread(new ThreadStart(Animation_Thread));
            thread1.Start();


            //アニメ1ステップ要素数
            Anim1step_Load();
        }


        //Spriteクラス終了処理
        public static void End()
        {
            WasDisposed = true;
        }



        public static int Set(int imageHndl, int u, int v, int width, int height)
        {
            for (int i = 0; i < SPRITE_MAX; i++)
            {
                int n = (NextNum + i) % SPRITE_MAX;

                if (sprite[n].used == false)
                {
                    sprite[n] = new SpriteCompornent();
                    sprite_Z[n] = 0;

                    sprite[n].used = true;
                    sprite[n].image = imageHndl;
                    // imageHndlが-1で空スプライトの作成
                    sprite[n].U = u;
                    sprite[n].V = v;
                    sprite[n].Width = width;
                    sprite[n].Height = height;
                    NextNum = n + 1;
                    return n;
                }
            }

            return -1;
        }

        public static void Attribution(int n, int attribution)
        {
            sprite[n].Attribution = attribution;
        }

        public static void Image(int n, int imageHndl)
        {
            sprite[n].image = imageHndl;
        }

        public static void Image(int n, int U, int V)
        {
            sprite[n].U = U;
            sprite[n].V = V;
        }

        public static void Image(int n, int U, int V, int Width, int Heigth)
        {
            sprite[n].U = U;
            sprite[n].V = V;
            sprite[n].Width = Width;
            sprite[n].Height = Heigth;
        }

        public static void Image(int n, int imageHndl, int U, int V, int Width, int Heigth)
        {
            sprite[n].image = imageHndl;
            sprite[n].U = U;
            sprite[n].V = V;
            sprite[n].Width = Width;
            sprite[n].Height = Heigth;
            sprite[n].AnimDelete[AnimType_UV] = true;
        }


        public static void Offset(int n, float x, float y)
        {
            sprite[n].X = x;
            sprite[n].Y = y;
            sprite[n].AnimDelete[AnimType_XY] = true;
        }

        public static void Offset(int n, float x, float y, short z)
        {
            sprite[n].X = x;
            sprite[n].Y = y;
            sprite_Z[n] = z;
            sprite[n].AnimDelete[AnimType_XY] = true;
        }


        public static object Var(int n, string key)
        {
            return sprite[n].Dict[key];
        }





        //使用済処理
        public static void Clear(int n)
        {
            sprite[n].used = false;
            NextNum = n + 1;
        }
        public static void Clear()
        {
            for (int n = 0; n < SPRITE_MAX; n++)
            {
                sprite[n].used = false;
                NextNum = n + 1;
            }
        }



        //^^^^^^^ アニメ情報
        //アニメ情報を登録する際はここに加えてAnimUpdate系統の追加も必要
        //そして、この下のAnimメソッドのswitch構文にも処理を追加
        //さらに, アニメでない通常処理を行ってパラメーターを変更した際, 自動でアニメのdeleteフラグをtrueにする必要がある
        public const int AnimType_XY = 1;
        public const int AnimType_UV = 2;

        private static void Anim1step_Load()
        {
            Anim1step[AnimType_XY] = 3;
            Anim1step[AnimType_UV] = 3;
        }
        //-------



        public static readonly int[] Anim1step = new int[SpriteCompornent.ANIM_MAX + 1]; //アニメの1ステップ要素数


        public static void Anim(int n, int Type, params int[] data)
        {
            if (Type < 0)
            {
                Type *= -1;
                sprite[n].AnimRelative[Type] = true;
            }
            else
            {
                sprite[n].AnimRelative[Type] = false;
            }
            sprite[n].AnimFrag[Type] = true;
            sprite[n].AnimDelete[Type] = false;


            //初期情報登録

            sprite[n].AnimData[Type] = new List<long>(0);

            switch (Type)
            {
                case AnimType_XY:
                    sprite[n].AnimData[Type].Add(0);
                    sprite[n].AnimData[Type].Add(sprite[n].AnimRelative[Type] ? 0 : (long)sprite[n].X);
                    sprite[n].AnimData[Type].Add(sprite[n].AnimRelative[Type] ? 0 : (long)sprite[n].Y);
                    break;
                case AnimType_UV:
                    sprite[n].AnimData[Type].Add(0);
                    sprite[n].AnimData[Type].Add(sprite[n].AnimRelative[Type] ? 0 : (long)sprite[n].U);
                    sprite[n].AnimData[Type].Add(sprite[n].AnimRelative[Type] ? 0 : (long)sprite[n].V);
                    break;
            }



            //アニメデータ引数を登録
            for (int i = 0; i < data.Length; i++)
            {
                sprite[n].AnimData[Type].Add(data[i]);
            }



            //回数が省略されていれば自動で1にする
            if (data.Length % Anim1step[Type] == 0) sprite[n].AnimData[Type].Add(1);


        }

        //アニメ動作中か調べる
        public static bool AnimCheck(int n)
        {
            for (int i = 1; i < SpriteCompornent.ANIM_MAX; i++)
            {
                if (sprite[n].AnimFrag[i]) return true;
            }
            return false;
        }





        //スプライト当たり判定
        public static int HitRectangle(int x, int y, int width, int height, uint mask)
        {
            Rectangle r1 = new Rectangle(x, y, width, height);
            for (int i=0; i<SPRITE_MAX; i++)
            {
                if (sprite[i].used && (sprite[i].mask & mask)!=0)
                {
                    Rectangle r2 = new Rectangle((int)sprite[i].X, (int)sprite[i].Y, sprite[i].Width, sprite[i].Height);

                    if (r1.IntersectsWith(r2)) return i;
                }
            }

            return -1;
        }

        public static int HitRectangle(int min, int max, int x, int y, int width, int height, uint mask)
        {
            Rectangle r1 = new Rectangle(x, y, width, height);
            for (int i = min; i < max; i++)
            {
                if (sprite[i].used && (sprite[i].mask & mask) != 0)
                {
                    Rectangle r2 = new Rectangle((int)sprite[i].X, (int)sprite[i].Y, sprite[i].Width, sprite[i].Height);

                    if (r1.IntersectsWith(r2)) return i;
                }
            }

            return -1;
        }







        //スプライト一括更新処理
        public static void AllUpdate()
        {
            for (int i = 0; i < SPRITE_MAX; i++)
            {
                if (sprite[i].used) sprite[i].Update(i);
            }
        }






        //スプライト一括描画処理
        public static void Drawing()
        {
            var draws = sprite_Z.OrderByDescending((x) => x.Value);

            //for (int i=0; i<SPRITE_MAX; i++)
            foreach (var v in draws)
            {
                int i = v.Key;

                if (sprite[i].used)
                {
                    if (sprite[i].image == -1) continue;

                    DX.DrawRectGraph((int)sprite[i].X, (int)sprite[i].Y, sprite[i].U, sprite[i].V,
                        sprite[i].Width, sprite[i].Height, sprite[i].image,
                        DX.TRUE, sprite[i].Attribution & Attribution_reverse);
                }
            }
        }



        //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ アニメーションスレッド (完全に指定FPS通りに処理はしていない)
        private static void Animation_Thread()
        {
            int frame = 0;
            int before = Environment.TickCount;


            while (!WasDisposed)
            {
                int now = Environment.TickCount;
                int progress = now - before;

                int ideal = (int)(frame * 1000.0F / ThreadFPS);

                //^^^^^^^^^^ 処理
                for (int i = 0; i < SPRITE_MAX; i++)
                {
                    if (sprite[i].used)
                    {
                        for (int j = 0 + 1; j < SpriteCompornent.ANIM_MAX + 1; j++)
                        {
                            if (sprite[i].AnimFrag[j])
                            {
                                if (sprite[i].AnimDelete[j]) //アニメデータ強制削除
                                {
                                    sprite[i].AnimData[j] = new List<long>(0);
                                    sprite[i].AnimFrag[j] = false;
                                    continue;
                                }

                                AnimUpdateBase(i, j); //iが管理番号, jがアニメタイプ
                            }
                        }
                    }
                }
                //---------

                if (ideal > progress) Thread.Sleep(ideal - progress);

                frame++;
                if (progress >= 1000) //1sごとに更新
                {
                    before = now;
                    frame = 0;
                }
            }

            //^^^^^^^^^^ アニメ更新処理
            void AnimUpdateBase(int n, int type)
            {
                bool smooth = false;
                int count = sprite[n].AnimCount[type];
                int step = sprite[n].AnimStep[type];

                if (count >= Math.Abs((int)sprite[n].AnimData[type][step * Anim1step[type]])) //次のコマへ移行
                {
                    step++;
                    count = 0;
                    int datalen = sprite[n].AnimData[type].Count;
                    int stepmax = (datalen - 1) / Anim1step[type] - 1;

                    if (step > stepmax) //1ループ終了

                    {
                        if (sprite[n].AnimData[type][datalen - 1] > 0) //ループ数が有限なら
                        {
                            sprite[n].AnimData[type][datalen - 1]--;
                            if (sprite[n].AnimData[type][datalen - 1] == 0) //ループ終了したらアニメデータを削除
                            {
                                sprite[n].AnimData[type] = new List<long>(0);
                                sprite[n].AnimFrag[type] = false;
                                return;
                            }
                        }
                        step = 0;

                    }
                }
                else
                {
                    count++;
                }

                int nextc = (int)sprite[n].AnimData[type][step * Anim1step[type]];
                if (nextc < 0) //スムーズアニメ
                {
                    nextc *= -1;
                    smooth = true;
                }

                //Console.WriteLine(step);
                switch (type)
                {
                    case AnimType_XY:
                        AnimUpdate_XY(); break;
                    case AnimType_UV:
                        AnimUpdate_UV(); break;

                }



                sprite[n].AnimCount[type] = count;
                sprite[n].AnimStep[type] = step;
                //ここでAnimUpdateBaseの処理は終了



                //^^^^^^^^^ 各々アニメ型に応じて場合分け
                //メソッドを追加したら上のswitch構文でちゃんと飛んでいけるようにすること
                void AnimUpdate_XY()
                {
                    int rx = 0, ry = 0;
                    if (sprite[n].AnimRelative[type])
                    {
                        rx = (int)sprite[n].AnimData[type][1];
                        ry = (int)sprite[n].AnimData[type][2];

                    }


                    int x2 = (int)sprite[n].AnimData[type][step * 3 + 1] + rx;
                    int y2 = (int)sprite[n].AnimData[type][step * 3 + 2] + ry;

                    if (smooth) //スムーズアニメなら
                    {
                        int backstep = step - 1;
                        if (step == 0) backstep = 0;

                        int x1 = (int)sprite[n].AnimData[type][backstep * 3 + 1] + rx;
                        int y1 = (int)sprite[n].AnimData[type][backstep * 3 + 2] + ry;

                        sprite[n].X = x1 + (float)((x2 - x1) * ((double)count / nextc));
                        //Console.WriteLine(nextc);
                        sprite[n].Y = y1 + (float)((y2 - y1) * ((double)count / nextc));
                    }
                    else //ラフアニメなら
                    {
                        if (count == 0)
                        {
                            sprite[n].X = x2;
                            sprite[n].Y = y2;
                        }
                    }
                }

                void AnimUpdate_UV()
                {
                    int ru = 0, rv = 0;
                    if (sprite[n].AnimRelative[type])
                    {
                        ru = (int)sprite[n].AnimData[type][1];
                        rv = (int)sprite[n].AnimData[type][2];

                    }

                    //Console.WriteLine(step);
                    int u2 = (int)sprite[n].AnimData[type][step * 3 + 1] + ru;
                    int v2 = (int)sprite[n].AnimData[type][step * 3 + 2] + rv;

                    if (smooth) //スムーズアニメなら
                    {
                        int backstep = step - 1;
                        if (step == 0) backstep = 0;

                        int u1 = (int)sprite[n].AnimData[type][backstep * 3 + 1] + ru;
                        int v1 = (int)sprite[n].AnimData[type][backstep * 3 + 2] + rv;

                        sprite[n].U = u1 + (int)((u2 - u1) * ((double)count / nextc));
                        sprite[n].V = v1 + (int)((v2 - v1) * ((double)count / nextc));
                    }
                    else //ラフアニメなら
                    {
                        if (count == 0)
                        {
                            sprite[n].U = u2;
                            sprite[n].V = v2;
                        }
                    }
                }

                //-----------

            }

        }




        // ------------------------------------------ アニメーションスレッド

    }

    //-------------------------------------------- class Sprite

    //お役立ちクラス
    static class Useful
    {
        //2値の間にあるかどうか
        public static bool between(double a, double min, double max)
        {
            if (min <= a && a <= max)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //スプライトを放物運動アニメさせる
        public static void Sprite_ParabolaAnim(int sp, int x1, int y1, int x2, int y2, int interval, double curv)
        {
            int w = x2 - x1, h = y2 - y1;

            int[] AnimData = new int[16 * 3];

            for (int i = 0; i < 16; i++)
            {
                AnimData[i * 3 + 0] = -interval;
                AnimData[i * 3 + 1] = x1 + (int)(w * (double)(i + 1) / 16);
                AnimData[i * 3 + 2] = y1 + (int)(h * (double)(i + 1) / 16 + curv * ((i + 1 - 8) * (i + 1 - 8) - 64));
            }

            Sprite.Anim(sp, Sprite.AnimType_XY, AnimData);
        }

        //簡易破壊エフェクト
        public static void Clash_effect(int imageHndl, int x0, int y0)
        {
            int sp;
            int w = 16; int h = 16;

            for (int i = 0; i < 4; i++)
            {
                int u = (i % 2) * (w / 2);
                int v = (int)(i / 2) * (w / 2);

                sp = Sprite.Set(imageHndl, u, v, w / 2, h / 2);
                Sprite.Offset(sp, x0 + u, y0 + v, -512);

                int x1 = x0 + (-w / 4 + u) * 3;
                int y1 = y0 + (-h / 4 + v) * 3 + h;
                Sprite_ParabolaAnim(sp, x0, y0, x1, y1, 2, 0.4);

                Sprite.sprite[sp].Update += new SpriteCompornent.UpdateDelegate(Sprite_Effeectfade); //削除処理追加
            }

        }


        public static void Sprite_Effeectfade(int sp)
        {
            if (!Sprite.AnimCheck(sp)) Sprite.Clear(sp);
        }

        public static void Sprite_EffeectfadeXY(int sp)
        {
            if (!Sprite.sprite[sp].AnimFrag[Sprite.AnimType_XY]) Sprite.Clear(sp);
        }



        public static void DrawString_shadow(int x, int y, string s)
        {
            DX.DrawString(x, y + 1, s, DX.GetColor(32, 32, 32));
            DX.DrawString(x, y, s, DX.GetColor(255, 255, 255));
        }

        public static void DrawString_bordered(int x, int y, string s)
        {
            uint c1 = DX.GetColor(255, 255, 255);
            uint c2 = DX.GetColor(32,32,32);
            DX.ChangeFontType(DX.DX_FONTTYPE_EDGE);
            DX.DrawString(x, y, s, c1,c2);
        }
        public static void DrawString_bordered(int x, int y, string s, uint c1)
        {
            uint c2 = DX.GetColor(32, 32, 32);
            DX.ChangeFontType(DX.DX_FONTTYPE_EDGE);
            DX.DrawString(x, y, s, c1, c2);
        }

        public static void DrawString_bordered(int x, int y, string s, uint c1, uint c2)
        {
            DX.ChangeFontType(DX.DX_FONTTYPE_EDGE);
            DX.DrawString(x, y, s, c1, c2);
        }





        //接触スプライトの中で一番手前にあるスプライトを返す
        public static int Sprite_HitRectangle_front(int x, int y, int width, int height, uint mask)
        {
            int ret = -1, next = 0;

            int h=0;
            while (h!=-1 && next< Sprite.SPRITE_MAX)
            {
                h = Sprite.HitRectangle(next, Sprite.SPRITE_MAX, x, y, width, height, mask);
                if (h!=-1)
                {
                    next = h + 1;

                    if (ret == -1)
                    {
                        ret = h;
                        continue;
                    }

                    if (Sprite.sprite_Z[ret] > Sprite.sprite_Z[h]) ret = h;
                }
            }


            return ret;
        }












        
        public static void Wait(int frame)
        {
            int i=0;
            DX.ScreenFlip();
            DX.SetDrawScreen(DX.DX_SCREEN_BACK);
            DX.DrawGraph(0,0,DX.DX_SCREEN_FRONT,0);

            while (DX.ProcessMessage() != -1)
            {
                DX.ScreenFlip();
                i++;
                if (i >= frame) return;
            }
        }
        


        /*
        public static void SpriteDict_Read(int sp, params object[] args)
        {
            for (int i=0; i<args.Length/2; i++)
            {
                
            }
        }
        */







    }


    //入力クラス
    static class Input
    {

        public static uint ClickLeft_time;


        public static void Start()
        {
            ClickLeft_time = 0;
        }

        public static void Update()
        {
            if ((DX.GetMouseInput() & DX.MOUSE_INPUT_LEFT) != 0)
            {
                ClickLeft_time += 1;
            }
            else
            {
                ClickLeft_time = 0;
            }
        }


        public static bool Left()
        {
            if (DX.CheckHitKey(DX.KEY_INPUT_A) == DX.TRUE || DX.CheckHitKey(DX.KEY_INPUT_LEFT) == DX.TRUE) return true;
            return false;
        }
        public static bool Right()
        {
            if (DX.CheckHitKey(DX.KEY_INPUT_D) == DX.TRUE || DX.CheckHitKey(DX.KEY_INPUT_RIGHT) == DX.TRUE) return true;
            return false;
        }
        public static bool Up()
        {
            if (DX.CheckHitKey(DX.KEY_INPUT_W) == DX.TRUE || DX.CheckHitKey(DX.KEY_INPUT_UP) == DX.TRUE) return true;
            return false;
        }
        public static bool Down()
        {
            if (DX.CheckHitKey(DX.KEY_INPUT_S) == DX.TRUE || DX.CheckHitKey(DX.KEY_INPUT_DOWN) == DX.TRUE) return true;
            return false;
        }

    }

}




