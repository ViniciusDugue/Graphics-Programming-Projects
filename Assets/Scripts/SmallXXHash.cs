
// its readonly so the methods dont adjust accumulator value they are called on
// this makes accumulator value immutable
public readonly struct SmallXXHash {

    const uint primeA = 0b10011110001101110111100110110001;
    const uint primeB = 0b10000101111010111100101001110111;
    const uint primeC = 0b11000010101100101010111000111101;
    const uint primeD = 0b00100111110101001110101100101111;
    const uint primeE = 0b00010110010101100110011110110001;
    readonly uint accumulator;

    //uint accumulator constructor method
    public SmallXXHash (uint accumulator) {
		this.accumulator = accumulator;
	}

    // Seed method that returns a seed plus a prime
    public static SmallXXHash Seed (int seed) => (uint)seed + primeE;

    // consuming data in groups of 32 bits 
    // =>: says this method has a single expression and the result of the expression is 
    //whats returned(implicit cast operator)
    public SmallXXHash Eat (int data) =>
		RotateLeft(accumulator + (uint)data * primeC, 17) * primeD;

	public SmallXXHash Eat (byte data) =>
		RotateLeft(accumulator + data * primeE, 11) * primeA;

    // creates avalanche value with avalanche effect
    // operator that implicitly/automatically converts the returned value to uint type
    //takes in a hash struct and returns the accumulator value from the struct
    public static implicit operator uint (SmallXXHash hash) 
    {
		uint avalanche = hash.accumulator;
		avalanche ^= avalanche >> 15;// shifted right 15 bits and xor with itself
		avalanche *= primeB; // multiplied by prime
		avalanche ^= avalanche >> 13; // shifted right 13 bits and xor with itself
		avalanche *= primeC; // multiled by prime
		avalanche ^= avalanche >> 16; //shifted right 16 bits and xor with itself 
		return avalanche;
	}
    // an implicit cast method for uint to small hash constructor
    // allows for: uint accumulatorValue = x;SmallXXHash hashInstance = accumulatorValue;
    public static implicit operator SmallXXHash (uint accumulator) =>
		new SmallXXHash(accumulator);
    
    // shifting bits causes bits on the left to be lost and are padedd with zeros on the right
    // rotating bits shifts them but the bits on the left are not lost and instead go to right
    // rotates bits of accumulator to the left by shifting all bits to the left with << and
    // adds the shifted bits to the right
    static uint RotateLeft (uint data, int steps) => (data << steps) | (data >> 32 - steps);
}





