 
 # ML22/23-1 Scalar Encoder with Buckets
 
 
 Group Name: Team_SpiralGanglions
 [Main Branch]( https://github.com/sahithkumar1999/neocortexapi_Team_SpiralGanglion/tree/Team_SpiralGanglions), But we had faced some problems in test cases so we continued the work in [another Branch](https://github.com/sahithkumar1999/neocortexapi_Team_SpiralGanglion/tree/test-cases) --> fully working code is there in this branch


 Program Links:
  Link for [Scalar Encoder](https://github.com/sahithkumar1999/neocortexapi_Team_SpiralGanglion/blob/test-cases/source/NeoCortexApi/Encoders/ScalarEncoder.cs)
  
  Link for [EncoderBaseClass](https://github.com/sahithkumar1999/neocortexapi_Team_SpiralGanglion/blob/test-cases/source/NeoCortexApi/Encoders/EncoderBase.cs )
  
  Link for [Test Cases]( https://github.com/sahithkumar1999/neocortexapi_Team_SpiralGanglion/blob/test-cases/source/UnitTestsProject/EncoderTests/ScalarEncoderTests.cs)


### Abstract:
In many machine learning applications, encoding data is an important step in preparing the data for analysis. The Scalar Encoder is a commonly used method 
for encoding numerical data, but it has limitations in terms of precision and flexibility. In this project, we implemented an improved version of the Scalar 
Encoder called the Scalar Encoder with Buckets. This method maps input values to continuous ranges of buckets, providing better precision in encoding data 
compared to the Scalar Encoder. It also automatically sets parameters based on the input data and supports periodic encoding of values, increasing the flexibility 
of the encoding process. We evaluated the performance of the Scalar Encoder with Buckets on various datasets and compared it to the Scalar Encoder, demonstrating 
its superior performance in terms of precision and flexibility.

### Introduction:
Data encoding is an important step in many machine learning applications, as it allows for efficient storage and analysis of data. The Scalar Encoder is a commonly 
used method for encoding numerical data, but it has limitations in terms of precision and flexibility. The Scalar Encoder maps input values to individual buckets, 
which can result in loss of precision when encoding data. Additionally, the Scalar Encoder requires manual specification of parameters such as the number of buckets 
and bucket size, which can make it inflexible and difficult to adapt to different datasets.

### Methodology:
To overcome the limitations of the Scalar Encoder, we implemented an improved version called the Scalar Encoder with Buckets. This method maps input values to 
continuous ranges of buckets, providing better precision in encoding data compared to the Scalar Encoder. The ClosenessScores method calculates the closeness score 
of an input value to each bucket range, indicating the level of activation for each bucket range. This provides a more precise encoding of input data compared to the 
Scalar Encoder, where the input values are mapped to individual buckets.

In addition to improved precision, the Scalar Encoder with Buckets also offers increased flexibility. The method automatically sets parameters such as the number of 
buckets and bucket size based on the input data, making it more flexible and easier to adapt to different datasets. The GenerateRangeDescription method generates a 
description of the bucket ranges used to encode the input data, which can help users understand the encoding scheme and adjust the parameters as needed. 
This increases the flexibility of the Scalar Encoder with Buckets and makes it easier to adapt to different datasets.

The Scalar Encoder with Buckets also supports periodic encoding of values, allowing for better handling of cyclical data. The GetTopDownMapping method generates a 
mapping of the bucket ranges to a hierarchy of levels, which can be useful for representing cyclical data such as time of day or day of the week. The GetBucketValues 
method returns the list of bucket values for a given bucket range, allowing users to encode cyclical data using a periodic encoding scheme.


Methods:
Below you can find the different methods and snippets of methods we added in-order to achieve the scalar encoder using buckets.

GetFirstOnBit: The bit offset of the first bit to be set in the encoder output is returned.
![image](https://user-images.githubusercontent.com/116685952/228794465-559b8f65-1a86-45ff-9f14-9303c1b4f2c2.png)

GetFirstOnBit for periodic 
![image](https://user-images.githubusercontent.com/116685952/228794814-1754da91-1bf6-4787-a5ea-13ed8bb60ab1.png)

GenerateRangeDescription: create a description from a range's text description.
![image](https://user-images.githubusercontent.com/116685952/228795326-5356407f-7bde-4cd7-999e-d1884baea741.png)

GetBucketIndex: Subclasses must override this returns a list of things, one for every bucket that this encoder has specified.
![image](https://user-images.githubusercontent.com/116685952/228795685-7cf8d192-5659-4e04-ba65-b51eb7e73c36.png)

GetBucketValue: Set the value of the bucket at the given index to the given value. A scalar encoder with buckets contains a GetBucketValues method that receives an input value and returns the bottom and upper bounds of the bucket in which it falls. 
![image](https://user-images.githubusercontent.com/116685952/228797322-c84568d0-5c59-4777-ac22-60330cb84621.png)

GetBucketInfo: The information about the associated bucket in the scalar encoder is included in an int array that is returned by the GetBucketInfo method, which accepts a double input value.
![image](https://user-images.githubusercontent.com/116685952/228797624-4d0fd8ad-6682-40fd-9d82-075881a9b61a.png)

![image](https://user-images.githubusercontent.com/116685952/228797719-6b9edded-3797-4f44-a25f-5c5d552eb166.png)

EncodeIntoArray: encodes input Data and writes the encoded value to the 1-D array of length .
![image](https://user-images.githubusercontent.com/116685952/228798115-c89fb6c3-2e91-42a8-905d-9b82c1113618.png)


ClosenessScores: Calculate ratings of proximity between the expected and actual scalar values.
![image](https://user-images.githubusercontent.com/116685952/228800430-0ba7647b-c78a-4b1b-9ec9-80bc71ba9abc.png)


Testcases Methods of Scalar Encoder:
ScalarEncodingGetBucketIndexPeriodic: These methods appear to be testing the Scalar Encoder class's GetBucketIndex method, which returns the bucket index for a specified scalar value. With a non-periodic encoder, the first method, ScalarEncodingGetBucketIndexNonPeriodic, generates a bitmap image for a set of scalar values and their corresponding bucket indices.
![image](https://user-images.githubusercontent.com/116685952/228801974-48984feb-6d26-4ffc-96c2-7929835bbca0.png)

TestGenerateRangeDescription: The Scalar Encoder class's GenerateRangeDescription method, which accepts a list of tuples representing ranges of values and produces a textual description of those ranges, is tested by the TestGenerateRangeDescription method.
![image](https://user-images.githubusercontent.com/116685952/228802227-61125ad0-099a-4b76-9771-68d289e9696a.png)


ClosenessScorestest: The Scalar Encoder class ClosenessScores method is being tested in this test. The Scalar Encoder is created with a set of parameters, two arrays of expected and actual values are defined, fractional is set to true, and the expected closeness score is set to 0.99. 
![image](https://user-images.githubusercontent.com/116685952/228802519-6e0f3250-d672-40cd-9e10-bf23403f98f1.png)


ScalarEncodingEncodeIntoArray: This test method examines the Scalar Encoder class EncodeIntoArray function. The method requires a Boolean indication indicating whether or not the encoder should learn, an output array, the length of the output array, and an integer input value. 
![image](https://user-images.githubusercontent.com/116685952/228802834-926dd43c-62ce-40c2-8ed3-68eec18b4015.png)

GetTopDownMapping: The top-down mapping of the input value to the encoder's buckets is represented via an integer array in this code by the private method _getTopDownMapping.
![image](https://user-images.githubusercontent.com/116685952/228803735-6e427ef6-dec9-445c-8565-bf19572ec3b3.png)

### Results:
We evaluated the performance of the Scalar Encoder with Buckets on various datasets and compared it to the Scalar Encoder. Our results showed that the Scalar 
Encoder with Buckets outperformed the Scalar Encoder in terms of precision and flexibility. The improved encoding scheme of the Scalar Encoder with Buckets 
provided better precision in encoding data compared to the Scalar Encoder, while the automatic setting of parameters and support for periodic encoding made 
it more flexible and easier to adapt to different datasets.

### Conclusion:
In conclusion, the Scalar Encoder with Buckets is an improved version of the Scalar Encoder that overcomes some of its limitations. It provides better precision 
in encoding data by mapping input values to continuous ranges of buckets, and supports periodic encoding of values for better handling of cyclical data. The automatic
setting of parameters based on the input data, as well as the generation of a bucket range description, increases the flexibility of the Scalar Encoder with Buckets 
and makes it easier to adapt to different datasets.

#### Link to project :
 Program links for different methods
    [ClosenessScores ](https://github.com/sahithkumar1999/neocortexapi_Team_SpiralGanglion/blob/cc85aed8060d240ea4ff818684f94add2d13bf5f/source/NeoCortexApi/Encoders/ScalarEncoder.cs#L701 ), 
    [EncodeIntoArray ](https://github.com/sahithkumar1999/neocortexapi_Team_SpiralGanglion/blob/cc85aed8060d240ea4ff818684f94add2d13bf5f/source/NeoCortexApi/Encoders/ScalarEncoder.cs#L346 ), 
    [decode ]( https://github.com/sahithkumar1999/neocortexapi_Team_SpiralGanglion/blob/cc85aed8060d240ea4ff818684f94add2d13bf5f/source/NeoCortexApi/Encoders/ScalarEncoder.cs#L241) ,
    [ GetBucketIndex](https://github.com/sahithkumar1999/neocortexapi_Team_SpiralGanglion/blob/cc85aed8060d240ea4ff818684f94add2d13bf5f/source/NeoCortexApi/Encoders/ScalarEncoder.cs#L475) ,
    [GetTopDownMapping ]( https://github.com/sahithkumar1999/neocortexapi_Team_SpiralGanglion/blob/cc85aed8060d240ea4ff818684f94add2d13bf5f/source/NeoCortexApi/Encoders/ScalarEncoder.cs#L817) ,
    [GenerateRangeDescription ]( https://github.com/sahithkumar1999/neocortexapi_Team_SpiralGanglion/blob/cc85aed8060d240ea4ff818684f94add2d13bf5f/source/NeoCortexApi/Encoders/ScalarEncoder.cs#L599 ) ,
    [GetBucketValues ]( https://github.com/sahithkumar1999/neocortexapi_Team_SpiralGanglion/blob/cc85aed8060d240ea4ff818684f94add2d13bf5f/source/NeoCortexApi/Encoders/ScalarEncoder.cs#L769 ),
    [GetBucketInfo](https://github.com/sahithkumar1999/neocortexapi_Team_SpiralGanglion/blob/3a2546872cbd9d96a7233fdd8125a1a7053b45b4/source/NeoCortexApi/Encoders/ScalarEncoder.cs#L527),

Forked from:
https://github.com/ddobric/neocortexapi

 Team Contribution link
       [Singari Sahith Kumar](https://github.com/sahithkumar1999/neocortexapi_Team_SpiralGanglion/commits/Singari-Sahith-Kumar?author=sahithkumar1999 ) ,
      [Anil Kumar Gadiraju](https://github.com/sahithkumar1999/neocortexapi_Team_SpiralGanglion/commits/Anilkumar?author=anilkumargadirajuFraUas),
      [Vinay Kumar Bandaru](https://github.com/sahithkumar1999/neocortexapi_Team_SpiralGanglion/commits/vinay-kumar-bandaru?author=Vinaykumarbandaru1999 ),

   Partially completed [Documentation](https://github.com/sahithkumar1999/neocortexapi_Team_SpiralGanglion/tree/test-cases/source/Documentation(Scalar%20Encoder%20With%20Buckets-%202022-2023))

   Link for test cases code [Click Here](https://github.com/sahithkumar1999/neocortexapi_Team_SpiralGanglion/blob/test-cases/source/UnitTestsProject/EncoderTests/ScalarEncoderTests.cs)




Test cases were passed
![image](https://user-images.githubusercontent.com/64829519/227833329-f7b5ef39-2cea-4de4-88a8-082a0b386775.png)

Periodic and Non-periodic test cases outputs for the method GetBucketIndex
![image](https://user-images.githubusercontent.com/64829519/227833634-ec274d8f-0046-41e6-a11a-3fac5363ccc4.png)

