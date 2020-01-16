public async Task Interface_Targetcue(float waitt_cue, int[] onecrossingPos, int[] wpointpos1, int[] wpointpos2)
        {/* async task for targetcue interface 

            Args:
                waitt_cue: wait time for cue interface (ms)
                onecrossingPos: the center position of the one crossing
                wpoint1pos, wpoint2pos: the positions of the two white points

            */


            using (var cancellationTokenSourece = new CancellationTokenSource())
            {
                var buttonTask = Task.Run(() =>
                {
                    DateTime startTime = DateTime.Now;
                    while (interupt_InterfaceOthers == false) ;

                    //
                    if (interupt_InterfaceOthers == true)
                    {
                        cancellationTokenSourece.Cancel();
                        interupt_InterfaceOthers = false;
                    }

                });

                try
                {
                    //myGrid.Children.Clear();
                    Remove_All();

                    // add one crossing on the right middle
                    Add_OneCrossing(onecrossingPos);
                    // two white points on left middle and top middle
                    Add_TwoWhitePoints(wpointpos1, wpointpos2);

                    textbox_thread.Text = "Targetcue running......";
                    interfaceState = InterfaceState.TargetCue;
                    // wait target cue for several seconds
                    await Wait_Interface(waitt_cue, cancellationTokenSourece.Token);
                    textbox_thread.Text = "Targetcue run completely";
                    
                }
                catch (TaskCanceledException)
                {
                    textbox_thread.Text = "Targetcue run cancelled";

                    Task task = null;
                    throw new TaskCanceledException(task);
                }

            }
        }