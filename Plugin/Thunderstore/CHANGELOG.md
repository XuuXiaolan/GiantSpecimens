## v 2.0.8 *bug fixes*
- Fixed mod not loading with LLL.

<details>
  <summary>Older Versions</summary>

## v 2.0.7 *bug fixes*
- Added particle system for driftwood and old bird being eaten.
- Added particle system for redwood dying and falling properly.
- Fixed dependencies.

## v 2.0.6 *configs*
- Added some config options
- Potentially fixed clients not being thrown up in the air.
- Potentially fixed client desync for scrap spawning after driftwood and redwood death.
- Fixed driftwood sample particles not stopping.
- New icon, fixed wrong sound for scream.
- Dislike how the lightning looks right now, will mess with it later.
- Missed with range and eyesight, let me know how the new values are like.
- Ported to LLL.

## v 2.0.5 *bug fixes*
- Prettying up the lightning
- Fixing config not working.

## v 2.0.4 *bug fixes*
- Potentially fixing problem with insta death with driftwood and player interactions.
- SHOULD fix driftwood targetting unkillable enemies because of course it did.
- Added more variant sounds for already existing sounds.
- Redwood fights old birds back again.
- Upped how easy it is for the driftwood to see you.
- Hitting the driftwood causes it to be much more aware of the player.
- Fixed a bug with being stunned.
- Fixed animation with slashes being stutter-y when animation is running away.

## v 2.0.2 *bug fixes*
- Reduced Zeus mode to spawn and death only.
- Will add a config for Zeus mode later and mess with it to look nice and sound smol.

## v 2.0.0 NEW ENEMY UPDATE
- Added Zeus, kinda, just keep an eye out for what the redwood does lmao.
- Fixed a few bugs that I can't fully remember.
- Added compatibility with hunter from LateGameUpgrades. (for their next big update, not this one).
- The Redwood Giant and Driftwood Giant spawn their heart on death.
- oh and added the driftwood giant lol.
- Fixed a few sound bugs.
- Added a new icon for the redwood plushie (someone come make a driftwood plushie thanks).

## v 1.9.0 V50 UPDATE
- Hopefully added a shader to improve particle system by now
- Fixed Whistle and plushie sounds being picked up/dropped being global.
- Allowed Redwood Giant to target old birds and Driftwood Giants.
- Fixed bug with Redwood Giant being stuck after eating a corpse of a ForestGiant.
- Added death animation for Redwood Giant.
- Compatible with Lethal Haptics
- You can get crushed by the Redwood Giant while they're falling during the death animation.

## v 1.8.0 *QoL + v50 compat*
- Added BMX-Lobby Compatibility for public lobbies.
- Added dependency -> EnumUtils, allows me to setup custom causes of death for when you scan dead players :3.
- Fixed plushie sounds being client-sided.
- Made mod compatible for v50.
- Improved footstep range colliders for shockwaves.
- Reworked all Redwood Giant animations and added a new one with a roar with its own custom sound.
- Added a ship hitbox that prevents giants from approaching it/dealing damage there.

## v 1.7.2 *QoL update*
- Updated dependency to latest LethalLib, meaning you can now specify to have the items and enemy spawn in.
- Cleaned up my code majorly
- Cleaned up config slightly.
- Finished making the model of the Driftwood, expect updates soon.
- Fixed terminal for whistle, no one told me it was broken smh, I just saw I used wrong code.
- Plushie sound added when squished.
- Fixed bug with dying inside ship with redwood giant hitting through wall.
- Wrote some basic AI for driftwood giant and some basic configs, ITS NOT OUT YET SO CONFIGS WONT DO ANYTHING.

## v 1.7.1 *bug fixes*
- Fixed Pathing being shown on player screen for Redwood Giant.
- Gave Whistle a scrap value when spawned as scrap.

## v 1.7.0 WHISTLE UPDATE!
- Adds a shop item whistle that attracts the RedWood Giant to your location.
- Added a cause of death for when you get squished to death by the RedWood Giant.
- Custom sounds and custom models made for the whistle.
- Credited Sintego for the plushie design.
- Fixed RedWood Giant from sliding around when grabbing Forest Giant sometimes
- Made Enable/Disable config for having whistle as a spawnable scrap.
- Made rarity config for whistle scrap.
- Good chance there's bugs, so report em.

## v 1.6.8  *bug fixes*
- Fixed scrap config rarity.
- Attached better model on the scrap.

## v 1.6.6 SCRAP UPDATE!
- Fixed Stomping being global to everyone after one person comes to range (please test and tell me how it goes).
- Added a RedWood Giant plushie scrap, worth a decent bit.
- Added config options for the scrap, including spawn levels.

## v 1.6.5 *QoL update*
- Working on navmesh stuff, should feel better walking around and not approach the ship really~.
- Made it glow less and gave it a darker shade of red.
- Added a config option for footstep dust colour (normal colours or white).

## v 1.6.4 *bug fixes*
- Fixed config not working properly, modded moons work again.

## v 1.6.3 ENEMY DAMAGE UPDATE!
- Footsteps can damage baboon hawks and dogs now.
- Colours feel smoother and the giant is easier to look at.
- Mostly QoL changes.

## v 1.6.2 CHAIN IK UPDATE!
- Honestly I dont know what it means really nor if it actually works, but now the giant kinda prefers stepping on you if you're close by.
- Footstep particles are MUCH cleaner, same with Particles after eating the forest giant.
- Slightly increased RedWood Giant speed of a default 1.5 to 2.0, it is also now a config option
- Added Blood splatter particles for when you get stepped on.
- Made it so that the RedWood Giant doesn't go too close to the ship and doesn't target forest keepers close to the ship.
- Other changes I forgot about because I'm silly but trust me there's a few more lol.

## v 1.5.3 *bug fixes*
- Fixed particle colours not working for any moon other than experimentation.
- Added Wesley's moons to configs (feel free to suggest next moons to add, has to be LE moons).
- Give feedback on the colours you see from the particles pls and ty.

## v 1.5.2 *bug fixes*
- Fixed left leg dust not showing up.

## v 1.5.0 PARTICLES UPDATE!
- Made the teeth glow a bit of a dark red.
- Updated the NavMesh Agent to make it behave like the Forest Keepers obstacle path.
- Updated the NavMesh Agent so it cant go near the player ship/inside, not sure which tbh.
- Added a bunch of configs for modded moon support and different vanilla moons support.
- Added Forest Keeper's death particles, Squishy blood particles from the player, Footstep dust particles with different colours for whatever material the RedWood Giant is stepping on.
- Removed animation desyncs when the Forest Keeper and the RedWood Giant spawned on top of eachother (??? yes I know).

## v 1.4.5 BESTIARY UPDATE!
- Gave the bestiary a high-quality rotating model (made by meatballdemon).
- Removed glowing eyes, feedback showed they sucked.
- Fixed insta-kill stepping collisions when the player was inside the ship.
- Generally refining stuff about the model moving around the terrain.
- Trying to make a particle system for after eating the forest keeper, probably next update.

## v 1.4.4 GLOWING EYE UPDATE!
- Gave it glowing red eyes.

## v 1.4.3 BIG PACKAGE UPDATE!
- Added the config option for spawnrate of the Pink giant, its recommended to keep it high ^^.
- Added the config option for the multiplier of forest keeper spawnrate increase after a RedWood Giant spawns.
- Added new amazing textures for the RedWood Giant (made by meatballdemon).
- Added spawning sound for the RedWood Giant.
- Re-enabled spawning because I somehow turned it off by accident (IM SORRY!).
- 1.4.3 because I messed up adding proper credits THREE TIMES to the texture creator :sob:.

## v 1.3.3 *Spawnrate hotfix*
- Changing some values around with spawnrate and audio.
- tell me how audio feels inside the facility.

## v 1.3.2 *Collision hotfix*
- Pink Giant no longer damages you if you're inside the ship.
- Might not go through narrow spaces anymore?

## v 1.3.1 *Minor update*
- Messing with spawn stuff, tell me if anything goes wrong.

## v 1.3.0 QUALITY UPDATE!
- Shockwaves are actually shockwaves now, so if a foot goes onto the ground it will damage you appropriately and feet in the air cant hurt you (unless you go directly under it and get stepped on).
- Visual to Audio footstep sync is 100% now, changed the audio system i was using and everything should be perfectly sync'd.
- Custom Forest Keeper screaming sound when being eaten!

## v 1.2.2 SPAWN ANIMATION UPDATE!
- The pink giant now has a spawn animation! woo! (its nothing impressive).
- Also the eating animation has been significantly improved, it looks like hes actually eating him now, getting closer inch by inch by the second.

## v 1.2.1 SQUISH UPDATE!
- Added Squishing sound for when you get stepped on.
- Changed bestiary name for when you look up "redwood giant".
- Updated the thunderstore changelog version titles.
- Removed Herobr- Wait no wrong mod.

## v 1.2.0 SCREAMING UPDATE!
- Added screen shaking, tell me how it feels, too intense, not intense enough, too short, too long, not enough distance, and so on.
- The forest giant now screams when picked up, yes.
- Don't forget to like and subscr- (share the mod please and ty).

## v 1.1.1 FAR AUDIO UPDATE!
- Fixed audio not working from far away, now be scared! terrified! hehe.

## v 1.1.0 SOUND UPDATE!
- Sounds are here! They're also in sync! (probably, please report if they're not).
- Sounds dont go too far, not sure how to make a similar effect to forest giant's steps.
- Made the giant go into idle animation for a bit after it eats a forest keeper.

## v 1.0.2 *Collision fix update*
- Made my own Collision script, fixed collision bugs with dealing damage on the entire body.
- Gave his steps "shockwaves" that deal lesser damage but extend from his feet.

## v 1.0.1 *Mod fix update*
- Added asset bundle, whoops.

## v 1.0.0 RELEAAASE!
- Release!

</details>