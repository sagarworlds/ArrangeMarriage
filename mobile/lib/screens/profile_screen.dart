import 'package:flutter/material.dart';

class ProfileScreen extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('My Profile')),
      body: Padding(
        padding: EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Full Name: John Doe', style: TextStyle(fontSize: 18)),
            Text('Age: 28', style: TextStyle(fontSize: 18)),
            Text('Religion: Hindu', style: TextStyle(fontSize: 18)),
            Text('Education: Bachelors in CS', style: TextStyle(fontSize: 18)),
            SizedBox(height: 20),
            ElevatedButton(onPressed: () {}, child: Text('Edit Profile')),
          ],
        ),
      ),
    );
  }
}
