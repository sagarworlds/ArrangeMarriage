import 'package:flutter/material.dart';
import '../services\api_service.dart';

class MatchScreen extends StatelessWidget {
  final ApiService apiService = ApiService();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Suggested Matches')),
      body: FutureBuilder(
        future: apiService.searchMatches('user-id'),
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) return Center(child: CircularProgressIndicator());
          if (snapshot.hasError) return Center(child: Text('Error loading matches'));

          final data = snapshot.data as dynamic; // This would be a list of Profiles in real app
          return ListView.builder(
            itemCount: 10, // Mocked count
            itemBuilder: (context, index) {
              return ListTile(
                title: Text('Candidate ${index + 1}'),
                subtitle: Text('Age: 25-30, Religion: Hindu'),
                trailing: ElevatedButton(
                  onPressed: () {
                    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Proposal Sent!')));
                  },
                  child: Text('Propose'),
                ),
              );
            },
          );
        },
      ),
    );
  }
}
