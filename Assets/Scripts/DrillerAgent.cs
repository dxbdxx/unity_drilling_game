using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;

public class DrillerAgent : Agent
{
    // General settings
    string project_direcotry = System.Environment.CurrentDirectory;
    DrillerAcademy academy;
    StreamWriter file_writer;
    float scale_real_world_shape = 100;     // fixed
    int FPS_show = 60;                      // fixed
    int n_record_eps = 10;                  // fixed

    // Environment settings
    public float depth_target;
    public float force_factor = 1f;
    public float friction_factor = 0.1f;
    public float limit_velocity_per_second = 20;

    float velocity_limited;
    float zone_search_upperbound;
    float zone_search_lowerbound;
    Vector3 local_coordinate;
    Vector3 length_environment;
    Vector3 confine;

    float length_agent;
    float length_target;
    float length_collision;

    // Attributes
    public Transform Target;
    public Transform Soil;
    public Transform DrillingPlatform;
    public Transform LocalCoordinate;

    int time;
    int n_episode;
    bool condition_fail;
    bool get_reward1;
    bool get_reward2;
    bool get_reward3;

    Vector3 relative_position;
    Vector3 velocity;
    Vector3 force;
    Vector3 previous_velocity;
    Vector3 direction;
    float previous_distance;
    float previous_y;

    // Initialization
    public override void InitializeAgent()
    {
        if (brain.brainType == BrainType.Show)
            this.enabled = false;

        print(Time.timeScale);
        print(Time.captureFramerate);
        print(Application.targetFrameRate);

        academy = FindObjectOfType<DrillerAcademy>();
        depth_target = academy.resetParameters["depth_target"];     // Curriculum Learning
        zone_search_upperbound = -depth_target + 15;
        zone_search_lowerbound = -depth_target - 15;

        velocity_limited = limit_velocity_per_second / FPS_show;
        local_coordinate = LocalCoordinate.position;
        length_environment = Soil.localScale;
        confine = length_environment / 2;
        length_target = Target.localScale.x;
        length_agent = this.transform.localScale.x;
        length_collision = (length_target + length_agent) * 0.9f;  // l*cuberoot(3)

        time = 0;
        n_episode = 0;
        condition_fail = false;
        get_reward1 = false;
        get_reward2 = false;
        get_reward3 = false;

        this.transform.position = DrillingPlatform.position;
        velocity = Vector3.zero;
        force = Vector3.zero;
        previous_velocity = Vector3.zero;
        previous_distance = float.MaxValue;
        previous_y = this.transform.position.y;
    }
    
    public override void AgentReset()
    {
        time = 0;
        n_episode += 1;

        get_reward1 = false;
        get_reward2 = false;
        get_reward3 = false;
        condition_fail = false;

        this.transform.position = DrillingPlatform.position;
        velocity = Vector3.zero;
        force = Vector3.zero;
        Target.position = local2world(new Vector3((Random.value * 1.6f - 0.8f) * confine.x, -depth_target, (Random.value * 1.6f - 0.8f) * confine.z));
        relative_position = Target.position - this.transform.position;

        // About recording the path information
        if (n_episode % n_record_eps == 0)
        {
            string dirname = project_direcotry + "\\data";
            string filename = dirname + "\\" + n_episode.ToString() + ".txt";
            Directory.CreateDirectory(dirname);
            FileStream t = File.Create(filename);
            file_writer = new StreamWriter(t, Encoding.UTF8);
            WriteData(0, 0, 0);
        }
    }

    public override void CollectObservations()
    {
        // Relative position
        AddVectorObs(relative_position.x / confine.x);
        AddVectorObs(relative_position.y / confine.y);
        AddVectorObs(relative_position.z / confine.z);

        // Distance to center of local
        AddVectorObs(world2local(this.transform.position.x,'x') / confine.x);
        AddVectorObs(world2local(this.transform.position.y,'y') / confine.y);
        AddVectorObs(world2local(this.transform.position.z,'z') / confine.z);

        // Agent velocity
        AddVectorObs(velocity.x / confine.x);
        AddVectorObs(velocity.y / confine.y);
        AddVectorObs(velocity.z / confine.z);
    }
    
    public override void AgentAction(float[] vectorAction, string textAction)
    {   
        // Timestamp
        time += 1;

        // Action means the force applied to the agent
        Vector3 control_signal = Vector3.zero;
        control_signal.x = Mathf.Clamp(vectorAction[0], -1, 1);
        control_signal.z = Mathf.Clamp(vectorAction[1], -1, 1);
        control_signal.y = Mathf.Clamp(vectorAction[2], -1, 1);

        // Movement
        force = force_factor * control_signal;
        velocity += force / FPS_show;
        /* if vectorAction[2] = 1??? */
        
        // Effect of friction and velocity limited
        if (velocity != Vector3.zero)
        {
            velocity -= friction_factor * force_factor * velocity.normalized / FPS_show;
            if (velocity.magnitude > velocity_limited)
                velocity = velocity_limited * velocity.normalized;
            this.transform.position += velocity;
            if (velocity != Vector3.zero)
                direction = velocity;
        }
        this.transform.forward = direction;

        // Deflection
        float delta_theta = Vector3.Angle(previous_velocity, velocity);
        if (force == Vector3.zero)
            delta_theta = 0;

        // Relative position
        relative_position = Target.position - this.transform.position;
        float distance_to_target = Vector3.Magnitude(relative_position);

        // Touch the target and finish the game
        if (distance_to_target < length_collision)
        {
            AddReward(1.5f);
            Done();
        }

        // Reward
        //print(distance_to_target);
        //print(get_reward1);
        //print(get_reward2);
        //print(n_episode);
        AddReward(reward_distance(distance_to_target, previous_distance));
        AddReward(reward_depth(this.transform.position.y, previous_y));
        AddReward(reward_time());
        if (velocity != Vector3.zero && previous_velocity != Vector3.zero)
            AddReward(reward_path(delta_theta));

        // Game over
        condition_fail = world2local(this.transform.position.x,'x') > confine.x ||
                         world2local(this.transform.position.x,'x') < -confine.x ||
                         world2local(this.transform.position.z,'z') > confine.z ||
                         world2local(this.transform.position.z,'z') < -confine.z ||
                         world2local(this.transform.position.y,'y') > 0 ||
                         world2local(this.transform.position.y,'y') < zone_search_lowerbound;
        if (condition_fail)
        {
            AddReward(-1.5f);
            Done();
        }

        // Update data
        previous_distance = distance_to_target;
        previous_velocity = velocity;
        previous_y = this.transform.position.y;

        // Debug and log
        Monitor.Log("time", time, MonitorType.text);
        Monitor.Log("cumulative reward", GetCumulativeReward(), MonitorType.text);
        Monitor.Log("reward", GetReward(), MonitorType.text);
        Monitor.Log("reward of path", reward_path(delta_theta), MonitorType.text);

        if (n_episode % n_record_eps == 0)
        {
            WriteData(GetReward(), GetCumulativeReward(), reward_path(delta_theta));
        }
    }

    public override void AgentOnDone()
    {
        if (file_writer != null) 
            file_writer.Close();
    }

    private Vector3 world2local(Vector3 world)
    {
        return world - local_coordinate;
    }

    private float world2local(float world, char d)
    {
        if (d == 'x')
            return world - local_coordinate.x;
        else if (d == 'y')
            return world - local_coordinate.y;
        else if (d == 'z')
            return world - local_coordinate.z;
        else
            throw new System.ArgumentException("Invalid argument");
    }

    private Vector3 local2world(Vector3 local)
    {
        return local + local_coordinate;
    }

    private float local2world(float world, char d)
    {
        if (d == 'x')
            return world + local_coordinate.x;
        else if (d == 'y')
            return world + local_coordinate.y;
        else if (d == 'z')
            return world + local_coordinate.z;
        else
            throw new System.ArgumentException("Invalid argument");
    }

    private float reward_distance(float d, float p_d)
    {
        float r = 0;

        float d1 = length_collision + depth_target / 2;
        float d2 = length_collision + depth_target / 5;
        float d3 = length_collision + depth_target / 10;

        if (d < d1 && !get_reward1 && this.transform.position.y < zone_search_upperbound) 
        {
            r += 0.2f;
            get_reward1 = true;
        }

        if (d < d2 && !get_reward2 && this.transform.position.y < zone_search_upperbound)
        {
            r += 0.5f;
            get_reward2 = true;
        }

        if (d < d3 && !get_reward3 && this.transform.position.y < zone_search_upperbound)
        {
            r += 0.5f;
            get_reward3 = true;
        }

        if (d < p_d)
        {
            r += 0.01f;
        }

        return r;
    }

    private float reward_depth(float y, float p_y)
    {
        float r = 0;

        if (y <= p_y && y > zone_search_upperbound)
        {
            // float r = 1/(depth_target / limit_velocity);
            r += 0.01f;
        }

        return r;
    }

    private float reward_time()
    {
        float r = 0;

        if (this.transform.position.y > zone_search_upperbound)
            r = -0.02f;
        else
            r = -0.01f;

        return r;
    }

    private float reward_path(float angle)
    {
        float r = 0;

        if (angle > 6)
            r += -0.01f;

        if (angle > 8)
            r += -0.02f;

        if (angle > 10)
            r += -0.03f;

        return r;
    }

    private void WriteData(float reward, float cumulative_reward, float reward_of_path)
    {
        string s = string.Format("{0}\t {1:0.000} {2:0.000} {3:0.000} {4:0.000} {5:0.000} {6:0.000} " +
                                "{7:0.000} {8:0.000} {9:0.000} {10} {11:0.000} {12}",
                                time,
                                this.transform.position.x,
                                this.transform.position.y,
                                this.transform.position.z,
                                this.transform.forward.x,
                                this.transform.forward.y,
                                this.transform.forward.z,
                                relative_position.x,
                                relative_position.y,
                                relative_position.z,
                                reward,
                                cumulative_reward,
                                reward_of_path);
        file_writer.WriteLine(s);
    }
}
