namespace MeteorPrinter.Enumerados
{
    public enum RepeatMode
    {
        /// <summary>
        /// Multiple copies of the image will be joined with a zero pixel gap
        /// 
        /// One product detect signal will initiate the printing of all copies of
        /// the image within the print job
        /// </summary>
        SEAMLESS = 0,
        /// <summary>
        /// Each copy of the image will require its own product detect signal
        /// 
        /// The minimum gap between product detects must leave sufficient margin
        /// for the image plus head X-direction span
        /// </summary>
        DISCRETE = 1
    }
}
