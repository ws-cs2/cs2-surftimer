using CounterStrikeSharp.API;

namespace WST;


public class OnAdvertisingTimerTickEvent {}
public class AdvertisingSystem: System
{

    private static readonly string[] _advertisements =
    {
        $" {CC.Main}[oce.surf] {CC.White}Finding this map too hard? Use {CC.Secondary}!servers{CC.White} to find an easier map or use {CC.Secondary}!styles{CC.White} to play on a easier style",
        $" {CC.Main}[oce.surf] {CC.White}Use {CC.Secondary}!styles {CC.White}to see fun styles (Turbo, Low Gravity, etc)",
        $" {CC.Main}[oce.surf] {CC.White}Bugged or cheated time? Report it on our discord {CC.Secondary}https://oce.surf/discord",
        $" {CC.Main}[oce.surf] {CC.White}Type {CC.Secondary}!help{CC.White} in chat for SurfTimer commands",
        $" {CC.Main}[oce.surf] {CC.White}Want to see how you stack up on the leaderboard? Try visiting our website {CC.Secondary}https://oce.surf",
        $" {CC.Main}[oce.surf] {CC.White}Type {CC.Secondary}!routes{CC.White} to see all the map stages and bonuses",
        $" {CC.Main}[oce.surf] {CC.White}Do {CC.Secondary}!lg{CC.White} to play the map in low gravity",
        $" {CC.Main}[oce.surf] {CC.White}Join our discord {CC.Secondary}https://oce.surf/discord",
        $" {CC.Main}[oce.surf] {CC.White}To see the top players by points type {CC.Secondary}!surftop{CC.White} in chat",
        $" {CC.Main}[oce.surf] {CC.White}To respawn type {CC.Secondary}!r{CC.White} in chat",
        $" {CC.Main}[oce.surf] {CC.White}Do {CC.Secondary}!turbo{CC.White} to play the map in Turbo mode (left click to go faster, right click to go slow)",
        $" {CC.Main}[oce.surf] {CC.White}Type {CC.Secondary}!replay{CC.White} in chat to see the best run on the current map",
        $" {CC.Main}[oce.surf] {CC.White}Want to support the server? All we ask is to {CC.Secondary}tell one friend{CC.White} about it and ask them to come play!",
    };
    
    public int CurrentAdvertisementIndex { get; set; } = 0;
    
    
    public AdvertisingSystem(EventManager eventManager, EntityManager entityManager, Database database) : base(eventManager, entityManager, database)
    {
        EventManager.Subscribe<OnAdvertisingTimerTickEvent>(SendAdvertismentToServer);
    }

    public void SendAdvertismentToServer(OnAdvertisingTimerTickEvent e)
    {
        var advertisement = _advertisements[CurrentAdvertisementIndex % _advertisements.Length];
        
        Server.NextFrame(() =>
        {
            Server.PrintToChatAll(advertisement);
        });
        

        CurrentAdvertisementIndex++;
        
        if (CurrentAdvertisementIndex >= _advertisements.Length)
        {
            CurrentAdvertisementIndex = 0;
        }
    }
 }