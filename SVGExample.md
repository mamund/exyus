It's easy to use **Exyus** to deliver SVG documents. It's just a matter of linking your existing SVG documents to an **Exyus** resource. Below is a quick example.

#### Create an SVG Document ####
```
<!DOCTYPE svg PUBLIC "-//W3C//DTD SVG 1.0//EN" 
 "http://www.w3.org/TR/2001/REC-SVG-20010904/DTD/svg10.dtd">

<svg xmlns="http://www.w3.org/2000/svg" 
   xmlns:xlink="http://www.w3.org/1999/xlink" 
   width='300px' height='300px'>

   <title>Small SVG example</title>
   <circle cx='120' cy='150' r='60' style='fill: gold;'>
       <animate attributeName='r' from='2' to='80' begin='0' dur='3' repeatCount='indefinite' />
   </circle>
   <polyline points='120 30, 25 150, 290 150' stroke-width='4' stroke='brown' style='fill: none;' />
   <polygon points='210 100, 210 200, 270 150' style='fill: lawngreen;' /> 
   <text x='60' y='250' fill='blue'>Hello, World!</text>

</svg>
```
#### Define an Exyus Resource ####
```
// return static svg document
[UriPattern(@"/samples/svg/(?:\.xcs)")]
[MediaTypes("image/svg+xml")]
public class svgHello : StaticResource
{
    public svgHello()
    {
        this.ContentType = "image/svg+xml";
        this.Content = Helper.ReadFile("/xcs/content/samples/hello.svg");
    }
}
```
That's all there is to it. You can view a liver version of this resource by pointing an SVG-compliant browser [here](http://exyus.com/xcs/samples/svg/).