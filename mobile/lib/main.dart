import 'package:flutter/material.dart';
import 'screens/login_screen.dart';

void main() {
  runApp(MatchMakerApp());
}

class MatchMakerApp extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'MatchMaker',
      theme: ThemeData(
        primarySwatch: Colors.red,
        visualDensity: VisualDensity.adaptivePlatformDensity,
      ),
      home: LoginScreen(),
    );
  }
}
