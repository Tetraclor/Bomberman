using Bomberman.Api;

namespace MyStrategy
{
    public static class Elements
    {
        public static Element[] FreeForHero = new Element[] { 
            Element.BOMBERMAN, 
            Element.BOMB_BOMBERMAN, 
            Element.Space
        };
        public static Element[] FreeForBoom = new Element[] { 
            Element.Space, 
            Element.BOMBERMAN, 
            Element.DeadMeatChopper, 
            Element.DEAD_BOMBERMAN, 
            Element.DestroyedWall, 
            Element.BOOM, 
            Element.OTHER_DEAD_BOMBERMAN 
        };
        public static Element[] Bombs = new Element[] { 
            Element.OTHER_BOMB_BOMBERMAN, 
            Element.BOMB_BOMBERMAN, 
            Element.BOMB_TIMER_1, 
            Element.BOMB_TIMER_2,
            Element.BOMB_TIMER_3,
            Element.BOMB_TIMER_4, 
            Element.BOMB_TIMER_5 
        };
        public static Element[] DestroedStatic = new Element[] {
            Element.BOMB_TIMER_1,
            Element.BOMB_TIMER_2, 
            Element.BOMB_TIMER_3, 
            Element.BOMB_TIMER_4, 
            Element.BOMB_TIMER_5,
            Element.DESTROYABLE_WALL,
            Element.OTHER_BOMB_BOMBERMAN, 
            Element.BOMB_BOMBERMAN
        };
        public static Element[] DestroedNotStatic = new Element[]
        {
            Element.BOMBERMAN,
            Element.OTHER_BOMBERMAN,
            Element.MEAT_CHOPPER
        };
    }
}
