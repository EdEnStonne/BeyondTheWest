using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BeyondTheWestTutorial.TutorialMenu;
using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine;

namespace BeyondTheWest.Tutorial;

public static class TutorialHook
{
    public const string BTWMovementSection = "Beyond The West Movement";
    public const string BTWSlugcatsSection = "Beyond The West Slugcats";
    public static void LoadTexture()
    {
        Futile.atlasManager.ActuallyLoadAtlasOrImage("BTWTutorial_BTWTech", "illustrations/Tutorial_BTWTech", "");

        Futile.atlasManager.ActuallyLoadAtlasOrImage("BTWTutorial_BTWSlugcat", "illustrations/Tutorial_BTWSlugcats", "");
        
        Plugin.Log("TutorialHook LoadTexture Done !");
    }
    public static void ApplyHooks()
    {
        InitBaseTutorialInfo();
        Plugin.Log("TutorialHook Hooks Done !");
    }

    public static void InitBaseTutorialInfo()
    {
        TutorialMenu.sectionsInfo.Add(new TutorialSectionInfo()
        {
            name = BTWMovementSection, 
            sectionImageName = "BTWTutorial_BTWTech", 
            description = "New movement modded in Beyond The West that everyone can do."
        });
        TutorialMenu.levelsInfo.Add(new TutorialLevelInfo()
        {
            name = "Pole Pounce", 
            section = BTWMovementSection, 
            level = "", 
            iconImageName = "", 
            canSelectSlugcat = true,
            description = "Pouncing on walls ? What about pouncing on poles ?<LINE>Trailseeker has another way to trigger this tech."
        });
        TutorialMenu.levelsInfo.Add(new TutorialLevelInfo()
        {
            name = "Pole Vertical Hop", 
            section = BTWMovementSection, 
            level = "", 
            iconImageName = "", 
            canSelectSlugcat = true,
            description = "You can now hop on vertical poles !<LINE>Again, precision is key to master this technique.<LINE>Trailseeker has a better affinity with this tech."
        });
        TutorialMenu.levelsInfo.Add(new TutorialLevelInfo()
        {
            name = "Pole Loop", 
            section = BTWMovementSection, 
            level = "", 
            iconImageName = "", 
            canSelectSlugcat = true,
            description = "This tech is more occassional and quite tricky in timing.<LINE>It does look really cool and can be useeful though !<LINE>Trailseeker and Rivulet have a better affinity with this tech."
        });

        TutorialMenu.sectionsInfo.Add(new TutorialSectionInfo()
        {
            name = BTWSlugcatsSection, 
            sectionImageName = "BTWTutorial_BTWSlugcat", 
            description = "Techniques specific to the slugcats of Beyond The West."
        });
        TutorialMenu.levelsInfo.Add(new TutorialLevelInfo()
        {
            name = "Wall Climb", 
            section = BTWSlugcatsSection, 
            level = "", 
            slugcat = TrailseekerFunc.Trailseeker, 
            iconImageName = "", 
            description = "Iterators hate this one simple trick.<LINE>Scale walls with agility !"
        });
        TutorialMenu.levelsInfo.Add(new TutorialLevelInfo()
        {
            name = "Wall Vertical Pounce", 
            section = BTWSlugcatsSection, 
            level = "", 
            iconImageName = "", 
            slugcat = TrailseekerFunc.Trailseeker, 
            description = "And that's not it ! With bigger walls comes new tech,<LINE> requiring abit more timing for way more vertical liberties !"
        });
        TutorialMenu.levelsInfo.Add(new TutorialLevelInfo()
        {
            name = "Wall Kick", 
            section = BTWSlugcatsSection, 
            level = "", 
            iconImageName = "", 
            slugcat = TrailseekerFunc.Trailseeker, 
            description = "Now that you know how to go up the walls,<LINE> you'd need a way to go down."
        });
        TutorialMenu.levelsInfo.Add(new TutorialLevelInfo()
        {
            name = "Kick", 
            section = BTWSlugcatsSection, 
            level = "", 
            iconImageName = "", 
            slugcat = TrailseekerFunc.Trailseeker, 
            description = "Those claws can also be used for offense !<LINE>A cheap trick that can save many situations."
        });
        TutorialMenu.levelsInfo.Add(new TutorialLevelInfo()
        {
            name = "Meltdown", 
            section = BTWSlugcatsSection, 
            level = "", 
            iconImageName = "", 
            slugcat = CoreFunc.Core, 
            description = "Your cell is a vital part of your slugcat.<LINE>Here's how to preserve it."
        });
        TutorialMenu.levelsInfo.Add(new TutorialLevelInfo()
        {
            name = "Leap", 
            section = BTWSlugcatsSection, 
            level = "", 
            iconImageName = "", 
            slugcat = CoreFunc.Core, 
            description = "Now here's the fun part : leaping !"
        });
        TutorialMenu.levelsInfo.Add(new TutorialLevelInfo()
        {
            name = "Zero-G", 
            section = BTWSlugcatsSection, 
            level = "", 
            iconImageName = "", 
            slugcat = CoreFunc.Core, 
            description = "The best tool in your movement kit.<LINE>Do a flip and float away !"
        });
        TutorialMenu.levelsInfo.Add(new TutorialLevelInfo()
        {
            name = "Swirl", 
            section = BTWSlugcatsSection, 
            level = "", 
            iconImageName = "", 
            slugcat = CoreFunc.Core, 
            description = "The way to go in low gravity environment !<LINE>Activating it in normal condition is tricky, but can reveal itself useful."
        });
        TutorialMenu.levelsInfo.Add(new TutorialLevelInfo()
        {
            name = "Charge and Discharge", 
            section = BTWSlugcatsSection, 
            level = "", 
            iconImageName = "", 
            slugcat = SparkFunc.Spark, 
            description = "The tricky but basic part of Spark's ability.<LINE>A great way to stun your foe."
        });
        TutorialMenu.levelsInfo.Add(new TutorialLevelInfo()
        {
            name = "Spear Throw", 
            section = BTWSlugcatsSection, 
            level = "", 
            iconImageName = "", 
            slugcat = SparkFunc.Spark, 
            description = "Your probably noticed that Spark's spear throw is weird.<LINE>It is because it depends on its charge."
        });
        TutorialMenu.levelsInfo.Add(new TutorialLevelInfo()
        {
            name = "Quick Slide & Roll Back", 
            section = BTWSlugcatsSection, 
            level = "", 
            iconImageName = "", 
            slugcat = SparkFunc.Spark, 
            description = "Zoom in, zoom out, the basics of Spark's movement !"
        });
        TutorialMenu.levelsInfo.Add(new TutorialLevelInfo()
        {
            name = "Bounce Up, Jump & Back", 
            section = BTWSlugcatsSection, 
            level = "", 
            iconImageName = "", 
            slugcat = SparkFunc.Spark, 
            description = "Some more advanced tech where you can utilize your electricity<LINE>to move around !"
        });
    }
}