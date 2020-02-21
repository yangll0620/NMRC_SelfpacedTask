******************
Presentation Class
******************
Namespace: GonoGoTask_wpfVer

Inheritance: System.Windows.Window



Properties
----------

Interface Related 

+-----------------+-------------+---------------------------------------------------------------------------------------------+
| t_Ready_List    | List<float> | len = ntrials, random t_Ready for each trial, generated in function Shuffle_GonogoTrials    |
+-----------------+-------------+---------------------------------------------------------------------------------------------+
| t_Cue_List      | List<float> | len = ntrials,  random t_Cue for each trial, generated in function Shuffle_GonogoTrials     |
+-----------------+-------------+---------------------------------------------------------------------------------------------+
| t_noGoShow_List | List<float> | len = ntrials, random t_noGoShow for each trial, generated in function Shuffle_GonogoTrials |
+-----------------+-------------+---------------------------------------------------------------------------------------------+


Touch Points Related 

+----------------+----------------+--------------------------------------------------+-----------------------------------------+
|       Property |      Data Type |                                                  | Used Function                           |
+----------------+----------------+--------------------------------------------------+-----------------------------------------+
|    tMax_1Touch |    List<float> | the Duration for One Touch (set 10ms)            |                                         |
+----------------+----------------+--------------------------------------------------+-----------------------------------------+
| touchPoints_Id |   HashSet<int> | Unique Touch Point Id within Each Touch Duration | Added/Removed: Touch_FrameReported()    |
+----------------+----------------+--------------------------------------------------+-----------------------------------------+
| downPoints_Pos | List<double[]> | the X, Y position of Each Touchdown Point        | Added: Touch_FrameReported()            |
|                |                | within Each Touch Duration (tMax_1Touch)         | Used/Removed: calc_GoTargetTouchState() |
+----------------+----------------+--------------------------------------------------+-----------------------------------------+


Methods 
-------

Wait_Reach():
	
	Output Member Variables:
		gotargetTouchstate: GoTargetTouchState.goHit, GoTargetTouchState.goClose, or GoTargetTouchState.goMiss




